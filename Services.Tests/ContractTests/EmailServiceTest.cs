using DataAccess.DataRequests;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class EmailServiceTest
    {
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private Mock<IEmailOperation> _emailOperationMock;
        private EmailService _emailService;

        [SetUp]
        public void Setup()
        {
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _emailOperationMock = new Mock<IEmailOperation>();
            _emailService = new EmailService(_playerRepositoryMock.Object, _emailOperationMock.Object);
        }

        [Test]
        public void SendVerificationCode_EmailVerification_EmailAvailable_SendsEmailAndReturnsSuccess()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode())
                .Returns("123456");
            _emailOperationMock.Setup(e => e.SendVerificationEmail(email, "123456", EmailType.EMAIL_VERIFICATION))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
            _emailOperationMock.Verify(e => e.SendVerificationEmail(email, "123456", EmailType.EMAIL_VERIFICATION), Times.Once);
        }

        [Test]
        public void SendVerificationCode_InvalidEmailFormat_ReturnsWrongData()
        {
            string invalidEmail = "invalid-email";
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(invalidEmail))
                .Returns(false);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.WRONG_DATA
            };

            var result = _emailService.SendVerificationCode(invalidEmail, EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendVerificationCode_EmailVerification_EmailAlreadyInUse_ReturnsUnallowed()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = true });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED
            };

            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendVerificationCode_EmailVerification_SendEmailFails_ReturnsServerError()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });
            _emailOperationMock.Setup(e => e.SendVerificationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailType>()))
                .Returns(false);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_EmailNotInUse_ReturnsNotFound()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_DbError_ReturnsServerError()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.DATABASE_ERROR
            };

            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_EmailExists_SendsEmailAndReturnsSuccess()
        {
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email))
                .Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode())
                .Returns("654321");
            _emailOperationMock.Setup(e => e.SendVerificationEmail(email, "654321", EmailType.PASSWORD_RESET))
                .Returns(true);
            _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = true });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            Assert.That(result.Equals(expected));
            _emailOperationMock.Verify(e => e.SendVerificationEmail(email, "654321", EmailType.PASSWORD_RESET), Times.Once);
        }

        [Test]
        public void ValidateVerificationCode_NoInfoInCache_ReturnsNotFound()
        {
            string email = GetUniqueEmail();
            ConfirmEmailRequest expected = new ConfirmEmailRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _emailService.ValidateVerificationCode(email, "000000", EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ValidateVerificationCode_ValidCode_ReturnsSuccess()
        {
            string email = GetUniqueEmail();
            string code = "111111";
            PrepareCacheWithCode(email, code, EmailType.EMAIL_VERIFICATION);
            ConfirmEmailRequest expected = new ConfirmEmailRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _emailService.ValidateVerificationCode(email, code, EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ValidateVerificationCode_InvalidCode_DecrementsAttemptsAndReturnsUnauthorized()
        {
            string email = GetUniqueEmail();
            string correctCode = "111111";
            PrepareCacheWithCode(email, correctCode, EmailType.EMAIL_VERIFICATION);
            ConfirmEmailRequest expected = new ConfirmEmailRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED,
                RemainingAttempts = 2
            };

            var result = _emailService.ValidateVerificationCode(email, "999999", EmailType.EMAIL_VERIFICATION);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ValidateVerificationCode_MaxAttemptsExceeded_CodeRemovedReturnsNotFound()
        {
            string email = GetUniqueEmail();
            string correctCode = "111111";
            PrepareCacheWithCode(email, correctCode, EmailType.PASSWORD_RESET);
            _emailService.ValidateVerificationCode(email, "Wrong1", EmailType.PASSWORD_RESET);
            _emailService.ValidateVerificationCode(email, "Wrong2", EmailType.PASSWORD_RESET);
            _emailService.ValidateVerificationCode(email, "Wrong3", EmailType.PASSWORD_RESET);
            ConfirmEmailRequest expected = new ConfirmEmailRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND,
                RemainingAttempts = 0
            };

            var resultAfterExhaustion = _emailService.ValidateVerificationCode(email, correctCode, EmailType.PASSWORD_RESET);

            Assert.That(resultAfterExhaustion.Equals(expected));
        }

        private static string GetUniqueEmail()
        {
            return $"{Guid.NewGuid()}@test.com";
        }

        private void PrepareCacheWithCode(string email, string code, EmailType type)
        {
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode()).Returns(code);
            _emailOperationMock.Setup(e => e.SendVerificationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailType>())).Returns(true);
            if (type == EmailType.EMAIL_VERIFICATION)
            {
                _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                    .Returns(new DataVerificationRequest { IsSuccess = false });
            }
            else if (type == EmailType.PASSWORD_RESET)
            {
                _playerRepositoryMock.Setup(d => d.VerifyEmailInUse(email))
                    .Returns(new DataVerificationRequest { IsSuccess = true });
            }
            _emailService.SendVerificationCode(email, type);
        }
    }
}
