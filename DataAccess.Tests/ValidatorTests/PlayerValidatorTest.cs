using DataAccess.Validators;
using DataAccess;
using NUnit.Framework;

namespace DataAccess.Test.ValidatorTests
{
    [TestFixture]
    public class PlayerValidatorTest
    {
        private Player _validPlayer;

        [SetUp]
        public void Setup()
        {
            // Initialize a valid player object before each test to ensure a clean state
            _validPlayer = new Player
            {
                username = "NoElTiki",
                name = "Ti",
                lastName = "Ki",
                facebookUsername = "NoEl.tiki",
                instagramUsername = "tikiIG",
                discordUsername = "tiki#1234",
                User = new User
                {
                    email = "tiki@estudiantes.uv.mx"
                }
            };
        }

        [Test]
        public void ValidatePlayerProfile_ValidPlayer_ReturnsTrue()
        {
            // Arrange
            // Setup() player is supposed to be valid

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePlayerProfile_NullPlayer_ReturnsFalse()
        {
            // Arrange
            Player player = null;

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(player);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_NullUser_ReturnsFalse()
        {
            // Arrange
            _validPlayer.User = null;

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePlayerProfile_InvalidEmail_ReturnsFalse(string email)
        {
            // Arrange
            _validPlayer.User.email = email;

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_EmailTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.User.email = new string('a', 31); // Max length is 30

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePlayerProfile_InvalidUsername_ReturnsFalse(string username)
        {
            // Arrange
            _validPlayer.username = username;

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_UsernameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.username = new string('a', 21); // Max length is 20

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_NameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.name = new string('a', 21); // Max length is 20

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_LastNameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.lastName = new string('a', 31); // Max length is 30

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_FacebookUsernameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.facebookUsername = new string('a', 31); // Max length is 30

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_InstagramUsernameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.instagramUsername = new string('a', 31); // Max length is 30

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_DiscordUsernameTooLong_ReturnsFalse()
        {
            // Arrange
            _validPlayer.discordUsername = new string('a', 31); // Max length is 30

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_OptionalFieldsNull_ReturnsTrue()
        {
            // Arrange
            _validPlayer.name = null;
            _validPlayer.lastName = null;
            _validPlayer.facebookUsername = null;
            _validPlayer.instagramUsername = null;
            _validPlayer.discordUsername = null;

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePlayerProfile_OptionalFieldsEmpty_ReturnsTrue()
        {
            // Arrange
            // Assuming empty strings are allowed for optional fields based on logic: !string.IsNullOrEmpty(...) checks length only if not empty.
            _validPlayer.name = "";
            _validPlayer.lastName = "";
            _validPlayer.facebookUsername = "";
            _validPlayer.instagramUsername = "";
            _validPlayer.discordUsername = "";

            // Act
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}
