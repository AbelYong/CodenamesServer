using DataAccess;
using DataAccess.DataRequests;
using DataAccess.Moderation;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using Services.Contracts.ServiceContracts.Managers;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Data;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class AuthenticationServiceTest
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<IBanDAO> _banDaoMock;
        private Mock<IEmailManager> _emailManagerMock;
        private AuthenticationService _authService;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _banDaoMock = new Mock<IBanDAO>();
            _emailManagerMock = new Mock<IEmailManager>();

            _authService = new AuthenticationService(
                _userRepositoryMock.Object,
                _banDaoMock.Object,
                _emailManagerMock.Object
            );
        }

        [Test]
        public void Authenticate_ValidCredentials_NoActiveBan_ReturnsSuccess()
        {
            string username = "validUser";
            string password = "validPassword";
            Guid userId = Guid.NewGuid();
            _userRepositoryMock.Setup(u => u.Authenticate(username, password))
                .Returns(userId);
            _banDaoMock.Setup(b => b.GetActiveBan(userId))
                .Returns((Ban)null);
            AuthenticationRequest expected = new AuthenticationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK,
                UserID = userId
            };

            var result = _authService.Authenticate(username, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Authenticate_ValidCredentials_ActiveBanExists_ReturnsAccountBanned()
        {
            string username = "bannedUser";
            string password = "password";
            Guid userId = Guid.NewGuid();
            var timeout = new DateTimeOffset();
            var activeBan = new Ban { timeout = timeout };
            _userRepositoryMock.Setup(u => u.Authenticate(username, password))
                .Returns(userId);
            _banDaoMock.Setup(b => b.GetActiveBan(userId))
                .Returns(activeBan);
            AuthenticationRequest expected = new AuthenticationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.ACCOUNT_BANNED,
                BanExpiration = timeout
            };

            var result = _authService.Authenticate(username, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Authenticate_InvalidCredentials_ReturnsUnauthorized()
        {
            string username = "wrongUser";
            string password = "wrongPassword";
            Guid userID = Guid.Empty;
            _userRepositoryMock.Setup(u => u.Authenticate(username, password))
                .Returns((Guid?)null);
            AuthenticationRequest expected = new AuthenticationRequest
            { 
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED,
                UserID = userID
            };

            var result = _authService.Authenticate(username, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Authenticate_EntityException_ReturnsServerError()
        {
            string username = "errorUser";
            string password = "password";
            _userRepositoryMock.Setup(u => u.Authenticate(username, password))
                .Throws(new EntityException("DB Connection failed"));
            AuthenticationRequest expected = new AuthenticationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.DATABASE_ERROR,
            };

            var result = _authService.Authenticate(username, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Authenticate_GeneralException_ReturnsServerError()
        {
            _userRepositoryMock.Setup(u => u.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Unexpected error"));
            AuthenticationRequest expected = new AuthenticationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.DATABASE_ERROR,
            };

            var result = _authService.Authenticate("user", "pass");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void CompletePasswordReset_ValidCode_UpdateSuccess_ReturnsUpdated()
        {
            string email = "test@test.com";
            string code = "123456";
            string newPass = "ValidPass123";
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(new ConfirmEmailRequest { IsSuccess = true });
            _userRepositoryMock.Setup(u => u.ResetPassword(email, newPass))
                .Returns(new UpdateRequest { IsSuccess = true });
            PasswordResetRequest expected = new PasswordResetRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.UPDATED,
            };

            var result = _authService.CompletePasswordReset(email, code, newPass);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void CompletePasswordReset_DbError_ServerError()
        {
            string email = "test@test.com";
            string code = "123456";
            string newPass = "ValidPass123";
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(new ConfirmEmailRequest { IsSuccess = true });
            _userRepositoryMock.Setup(u => u.ResetPassword(email, newPass))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR });
            PasswordResetRequest expected = new PasswordResetRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.DATABASE_ERROR,
            };

            var result = _authService.CompletePasswordReset(email, code, newPass);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void CompletePasswordReset_InvalidCode_ReturnsUnauthorizedPasswordUnchanged()
        {
            string email = "test@test.com";
            string code = "000000";
            string newPass = "NewPass123";
            int remainingAttempts = 2;
            var emailResponse = new ConfirmEmailRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED,
                RemainingAttempts = remainingAttempts
            };
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(emailResponse);
            PasswordResetRequest expected = new PasswordResetRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED,
                RemainingAttempts = remainingAttempts
            };

            var result = _authService.CompletePasswordReset(email, code, newPass);

            Assert.That(result.Equals(expected));
            _userRepositoryMock.Verify(u => u.ResetPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CompletePasswordReset_ValidCode_DbValidationFails_ReturnsWrongData()
        {
            string email = "test@test.com";
            string code = "123456";
            string newPass = "InvalidPass";
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(new ConfirmEmailRequest { IsSuccess = true });
            _userRepositoryMock.Setup(u => u.ResetPassword(email, newPass))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.INVALID_DATA });
            PasswordResetRequest expected = new PasswordResetRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.WRONG_DATA,
            };

            var result = _authService.CompletePasswordReset(email, code, newPass);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdatePassword_Success_ReturnsUpdated()
        {
            string username = "User1";
            string oldPass = "OldPass";
            string newPass = "NewPass";
            _userRepositoryMock.Setup(u => u.UpdatePassword(username, oldPass, newPass))
                .Returns(new UpdateRequest { IsSuccess = true });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.UPDATED,
            };

            var result = _authService.UpdatePassword(username, oldPass, newPass);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdatePassword_WrongCredentials_ReturnsUnauthorized()
        {
            string username = "User1";
            string oldPass = "WrongOldPass";
            string newPass = "NewPass";
            _userRepositoryMock.Setup(u => u.UpdatePassword(username, oldPass, newPass))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.UNALLOWED });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED
            };

            var result = _authService.UpdatePassword(username, oldPass, newPass);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdatePassword_DbError_ReturnsServerError()
        {
            _userRepositoryMock.Setup(u => u.UpdatePassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _authService.UpdatePassword("user", "old", "new");

            Assert.That(result.Equals(expected));
        }
    }
}
