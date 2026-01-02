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
        private Mock<IPlayerDAO> _playerDaoMock;
        private Mock<IEmailOperation> _emailOperationMock;
        private EmailService _emailService;

        [SetUp]
        public void Setup()
        {
            _playerDaoMock = new Mock<IPlayerDAO>();
            _emailOperationMock = new Mock<IEmailOperation>();
            _emailService = new EmailService(_playerDaoMock.Object, _emailOperationMock.Object);
        }

        #region SendVerificationCode Tests

        [Test]
        public void SendVerificationCode_InvalidEmailFormat_ReturnsWrongData()
        {
            // Arrange
            string invalidEmail = "invalid-email";
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(invalidEmail)).Returns(false);

            // Act
            var result = _emailService.SendVerificationCode(invalidEmail, EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.WRONG_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void SendVerificationCode_EmailVerification_EmailAlreadyInUse_ReturnsUnallowed()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);

            // Simulate that email is already taken in the DB
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = true });

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
        }

        [Test]
        public void SendVerificationCode_EmailVerification_EmailAvailable_SendsEmailAndReturnsOk()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode()).Returns("123456");
            _emailOperationMock.Setup(e => e.SendVerificationEmail(email, "123456", EmailType.EMAIL_VERIFICATION)).Returns(true);

            // Simulate that email is NOT taken (Success = false means not found/free for registration)
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
            _emailOperationMock.Verify(e => e.SendVerificationEmail(email, "123456", EmailType.EMAIL_VERIFICATION), Times.Once);
        }

        [Test]
        public void SendVerificationCode_EmailVerification_SendEmailFails_ReturnsServerError()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });

            // Simulate Email Operation failing (e.g. SMTP down)
            _emailOperationMock.Setup(e => e.SendVerificationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailType>()))
                .Returns(false);

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_EmailNotInUse_ReturnsNotFound()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);

            // Simulate email NOT found in DB
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false });

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_DbError_ReturnsNotFound()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);

            // Simulate DB Error
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR });

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void SendVerificationCode_PasswordReset_EmailExists_SendsEmailAndReturnsOk()
        {
            // Arrange
            string email = GetUniqueEmail();
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode()).Returns("654321");
            _emailOperationMock.Setup(e => e.SendVerificationEmail(email, "654321", EmailType.PASSWORD_RESET)).Returns(true);

            // Simulate email FOUND in DB
            _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                .Returns(new DataVerificationRequest { IsSuccess = true });

            // Act
            var result = _emailService.SendVerificationCode(email, EmailType.PASSWORD_RESET);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
            _emailOperationMock.Verify(e => e.SendVerificationEmail(email, "654321", EmailType.PASSWORD_RESET), Times.Once);
        }

        #endregion

        #region ValidateVerificationCode Tests

        [Test]
        public void ValidateVerificationCode_NoInfoInCache_ReturnsNotFound()
        {
            // Arrange
            string email = GetUniqueEmail();
            // We do NOT call SendVerificationCode, so cache is empty for this email

            // Act
            var result = _emailService.ValidateVerificationCode(email, "000000", EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void ValidateVerificationCode_ValidCode_ReturnsOk()
        {
            // Arrange
            string email = GetUniqueEmail();
            string code = "111111";
            PrepareCacheWithCode(email, code, EmailType.EMAIL_VERIFICATION);

            // Act
            var result = _emailService.ValidateVerificationCode(email, code, EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void ValidateVerificationCode_InvalidCode_DecrementsAttemptsAndReturnsUnauthorized()
        {
            // Arrange
            string email = GetUniqueEmail();
            string correctCode = "111111";
            PrepareCacheWithCode(email, correctCode, EmailType.EMAIL_VERIFICATION);

            // Act
            var result = _emailService.ValidateVerificationCode(email, "999999", EmailType.EMAIL_VERIFICATION);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));
            Assert.That(2.Equals(result.RemainingAttempts)); // Started with 3, failed once -> 2
        }

        [Test]
        public void ValidateVerificationCode_MaxAttemptsExceeded_ReturnsUnauthorized()
        {
            // Arrange
            string email = GetUniqueEmail();
            string correctCode = "111111";
            PrepareCacheWithCode(email, correctCode, EmailType.PASSWORD_RESET);

            // Act - Exhaust the 3 attempts
            _emailService.ValidateVerificationCode(email, "Wrong1", EmailType.PASSWORD_RESET); // 2 left
            _emailService.ValidateVerificationCode(email, "Wrong2", EmailType.PASSWORD_RESET); // 1 left
            var resultAttempt3 = _emailService.ValidateVerificationCode(email, "Wrong3", EmailType.PASSWORD_RESET); // 0 left

            // Try one more time (even with correct code, it should fail if logic strictly checks attempts)
            // Note: The logic in EmailService says "if (info.RemainingAttempts > 0) ... else { Returns UNAUTHORIZED }".
            // So on the 4th try it enters the else block.
            var resultAfterExhaustion = _emailService.ValidateVerificationCode(email, correctCode, EmailType.PASSWORD_RESET);

            // Assert
            Assert.That(resultAttempt3.IsSuccess, Is.False);
            Assert.That(0.Equals(resultAttempt3.RemainingAttempts));

            Assert.That(resultAfterExhaustion.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(resultAfterExhaustion.StatusCode));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Helper to generate unique emails so tests running in parallel (or sharing the static MemoryCache)
        /// do not collide with each other's verification codes.
        /// </summary>
        private static string GetUniqueEmail()
        {
            return $"{Guid.NewGuid()}@test.com";
        }

        /// <summary>
        /// Helper to populate the private static MemoryCache by invoking the public Send method.
        /// </summary>
        private void PrepareCacheWithCode(string email, string code, EmailType type)
        {
            _emailOperationMock.Setup(e => e.ValidateEmailFormat(email)).Returns(true);
            _emailOperationMock.Setup(e => e.GenerateSixDigitCode()).Returns(code);
            _emailOperationMock.Setup(e => e.SendVerificationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailType>())).Returns(true);

            if (type == EmailType.EMAIL_VERIFICATION)
            {
                // Ensure logic allows sending (Email NOT in use)
                _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                    .Returns(new DataVerificationRequest { IsSuccess = false });
            }
            else // PASSWORD_RESET
            {
                // Ensure logic allows sending (Email IS in use)
                _playerDaoMock.Setup(d => d.VerifyEmailInUse(email))
                    .Returns(new DataVerificationRequest { IsSuccess = true });
            }

            // Call the service to legitimately set the cache
            _emailService.SendVerificationCode(email, type);
        }

        #endregion
    }
}
