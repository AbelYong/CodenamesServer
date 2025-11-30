

using DataAccess.Validators;
using NUnit.Framework;

namespace DataAccess.Test.ValidatorTests
{
    public class UserValidatorTest
    {
        [TestCase("test.user@gmail.com")]
        [TestCase("user123@outlook.com")]
        [TestCase("zs12345621@estudiantes.uv.mx")]
        [TestCase("TEST.USER@GMAIL.COM")] // Case insensitivity check
        public void ValidateEmailFormat_ValidEmail_ReturnsTrue(string email)
        {
            // Act
            bool result = UserValidator.ValidateEmailFormat(email);

            // Assert
            Assert.That(result, Is.True);
        }

        [TestCase("user@yahoo.com")] // Invalid domain
        [TestCase("user@gmail")] // Incomplete domain
        [TestCase("user.gmail.com")] // Missing @
        [TestCase("@outlook.com")] // Missing local part
        [TestCase(null)]
        [TestCase("")]
        public void ValidateEmailFormat_InvalidEmail_ReturnsFalse(string email)
        {
            // Act
            bool result = UserValidator.ValidateEmailFormat(email);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_ValidPassword_ReturnsTrue()
        {
            // Arrange: Meets all criteria (10-16 chars, Upper, Lower, Digit, Special)
            string password = "Valid1Password!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePassword_TooShort_ReturnsFalse()
        {
            // Arrange: 9 characters (min is 10)
            string password = "Short1Pw!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_TooLong_ReturnsFalse()
        {
            // Arrange: 17 characters (max is 16)
            string password = "PasswordTooLong1!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoUpperCase_ReturnsFalse()
        {
            // Arrange
            string password = "nopassword1!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoLowerCase_ReturnsFalse()
        {
            // Arrange
            string password = "NOPASSWORD1!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoDigit_ReturnsFalse()
        {
            // Arrange
            string password = "NoDigitPassword!";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoSpecialChar_ReturnsFalse()
        {
            // Arrange
            string password = "NoSpecialChar1";

            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePassword_NullOrEmpty_ReturnsFalse(string password)
        {
            // Act
            bool result = UserValidator.ValidatePassword(password);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}
