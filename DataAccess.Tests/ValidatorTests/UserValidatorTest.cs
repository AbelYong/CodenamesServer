using DataAccess.Validators;
using NUnit.Framework;

namespace DataAccess.Test.ValidatorTests
{
    public class UserValidatorTest
    {
        [TestCase("test.user@gmail.com")]
        [TestCase("user123@outlook.com")]
        [TestCase("zs12345621@estudiantes.uv.mx")]
        [TestCase("TEST.USER@GMAIL.COM")]
        public void ValidateEmailFormat_ValidEmail_ReturnsTrue(string email)
        {
            bool result = UserValidator.ValidateEmailFormat(email);

            Assert.That(result, Is.True);
        }

        [TestCase("user@yahoo.com")]
        [TestCase("user@gmail")]
        [TestCase("user.gmail.com")]
        [TestCase("@outlook.com")]
        [TestCase(null)]
        [TestCase("")]
        public void ValidateEmailFormat_InvalidEmail_ReturnsFalse(string email)
        {
            bool result = UserValidator.ValidateEmailFormat(email);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_ValidPassword_ReturnsTrue()
        {
            string password = "Valid1Password!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePassword_TooShort_ReturnsFalse()
        {
            string password = "Short1Pw!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_TooLong_ReturnsFalse()
        {
            string password = "PasswordTooLong1!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoUpperCase_ReturnsFalse()
        {
            string password = "nouppercase1!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoLowerCase_ReturnsFalse()
        {
            string password = "UPPERPASSWORD1!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoDigit_ReturnsFalse()
        {
            string password = "NoDigitPassword!";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePassword_NoSpecialChar_ReturnsFalse()
        {
            string password = "NoSpecialChar1";

            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePassword_NullOrEmpty_ReturnsFalse(string password)
        {
            bool result = UserValidator.ValidatePassword(password);

            Assert.That(result, Is.False);
        }
    }
}
