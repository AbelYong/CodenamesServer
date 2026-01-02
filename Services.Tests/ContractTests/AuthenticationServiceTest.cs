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
        private Mock<IUserDAO> _userDaoMock;
        private Mock<IBanDAO> _banDaoMock;
        private Mock<IEmailManager> _emailManagerMock;
        private AuthenticationService _authService;

        [SetUp]
        public void Setup()
        {
            _userDaoMock = new Mock<IUserDAO>();
            _banDaoMock = new Mock<IBanDAO>();
            _emailManagerMock = new Mock<IEmailManager>();

            _authService = new AuthenticationService(
                _userDaoMock.Object,
                _banDaoMock.Object,
                _emailManagerMock.Object
            );
        }

        [Test]
        public void Authenticate_ValidCredentials_NoActiveBan_ReturnsSuccess()
        {
            // Arrange
            string username = "validUser";
            string password = "validPassword";
            Guid userId = Guid.NewGuid();

            // 1. Authenticate returns a valid ID
            _userDaoMock.Setup(u => u.Authenticate(username, password)).Returns(userId);

            // 2. No active ban found (returns null)
            _banDaoMock.Setup(b => b.GetActiveBan(userId)).Returns((Ban)null);

            // Act
            var result = _authService.Authenticate(username, password);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
            Assert.That(userId.Equals(result.UserID));
        }

        [Test]
        public void Authenticate_ValidCredentials_ActiveBanExists_ReturnsAccountBanned()
        {
            // Arrange
            string username = "bannedUser";
            string password = "password";
            Guid userId = Guid.NewGuid();
            var activeBan = new Ban(); // Assuming Ban entity has a default constructor

            // 1. Authenticate returns valid ID
            _userDaoMock.Setup(u => u.Authenticate(username, password)).Returns(userId);

            // 2. Ban IS found
            _banDaoMock.Setup(b => b.GetActiveBan(userId)).Returns(activeBan);

            // Act
            var result = _authService.Authenticate(username, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.ACCOUNT_BANNED.Equals(result.StatusCode));
        }

        [Test]
        public void Authenticate_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            string username = "wrongUser";
            string password = "wrongPassword";

            // Authenticate returns null
            _userDaoMock.Setup(u => u.Authenticate(username, password)).Returns((Guid?)null);

            // Act
            var result = _authService.Authenticate(username, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));
            Assert.That(result.UserID.Equals(Guid.Empty));
        }

        [Test]
        public void Authenticate_EntityException_ReturnsServerError()
        {
            // Arrange
            string username = "errorUser";
            string password = "password";

            // Simulate DB error
            _userDaoMock.Setup(u => u.Authenticate(username, password))
                .Throws(new EntityException("DB Connection failed"));

            // Act
            var result = _authService.Authenticate(username, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void Authenticate_GeneralException_ReturnsServerError()
        {
            // Arrange
            _userDaoMock.Setup(u => u.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Unexpected error"));

            // Act
            var result = _authService.Authenticate("user", "pass");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void CompletePasswordReset_InvalidCode_ReturnsErrorFromEmailManager()
        {
            // Arrange
            string email = "test@test.com";
            string code = "000000";
            string newPass = "NewPass123";

            // Simulate code validation failure (e.g. Wrong Code)
            var emailResponse = new ConfirmEmailRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED,
                RemainingAttempts = 2
            };

            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(emailResponse);

            // Act
            var result = _authService.CompletePasswordReset(email, code, newPass);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));
            Assert.That(2.Equals(result.RemainingAttempts));
            // Verify we didn't try to touch the DB
            _userDaoMock.Verify(u => u.ResetPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CompletePasswordReset_ValidCode_UpdateSuccess_ReturnsUpdated()
        {
            // Arrange
            string email = "test@test.com";
            string code = "123456";
            string newPass = "ValidPass123";

            // 1. Email validation success
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(new ConfirmEmailRequest { IsSuccess = true });

            // 2. DB Update success
            _userDaoMock.Setup(u => u.ResetPassword(email, newPass))
                .Returns(new UpdateRequest { IsSuccess = true });

            // Act
            var result = _authService.CompletePasswordReset(email, code, newPass);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.UPDATED.Equals(result.StatusCode));
        }

        [Test]
        public void CompletePasswordReset_ValidCode_DbUpdateFails_ReturnsMappedStatusCode()
        {
            // Arrange
            string email = "test@test.com";
            string code = "123456";
            string newPass = "InvalidPass";

            // 1. Email validation success
            _emailManagerMock.Setup(m => m.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET))
                .Returns(new ConfirmEmailRequest { IsSuccess = true });

            // 2. DB Update fails (e.g. Password policy violation triggers INVALID_DATA in DAO)
            _userDaoMock.Setup(u => u.ResetPassword(email, newPass))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.INVALID_DATA });

            // Act
            var result = _authService.CompletePasswordReset(email, code, newPass);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            // AuthenticationService maps ErrorType.INVALID_DATA -> StatusCode.WRONG_DATA
            Assert.That(StatusCode.WRONG_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void UpdatePassword_Success_ReturnsUpdated()
        {
            // Arrange
            string username = "User1";
            string oldPass = "OldPass";
            string newPass = "NewPass";

            _userDaoMock.Setup(u => u.UpdatePassword(username, oldPass, newPass))
                .Returns(new UpdateRequest { IsSuccess = true });

            // Act
            var result = _authService.UpdatePassword(username, oldPass, newPass);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.UPDATED.Equals(result.StatusCode));
        }

        [Test]
        public void UpdatePassword_WrongCredentials_ReturnsUnauthorized()
        {
            // Arrange
            string username = "User1";
            string oldPass = "WrongOldPass";
            string newPass = "NewPass";

            // Simulate DAO returning UNALLOWED (which implies auth failure in this context)
            _userDaoMock.Setup(u => u.UpdatePassword(username, oldPass, newPass))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.UNALLOWED });

            // Act
            var result = _authService.UpdatePassword(username, oldPass, newPass);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            // Mapped: UNALLOWED -> UNAUTHORIZED
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));
        }

        [Test]
        public void UpdatePassword_DbError_ReturnsServerError()
        {
            // Arrange
            _userDaoMock.Setup(u => u.UpdatePassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new UpdateRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR });

            // Act
            var result = _authService.UpdatePassword("user", "old", "new");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            // Mapped: DB_ERROR -> SERVER_ERROR
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }
    }
}
