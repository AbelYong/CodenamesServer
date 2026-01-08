using DataAccess.Validators;
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
            _validPlayer = new Player
            {
                username = "TestUsername",
                name = "Name",
                lastName = "Last name",
                facebookUsername = "test facebook",
                instagramUsername = "test IG",
                discordUsername = "test#1234",
                User = new User
                {
                    email = "test@estudiantes.uv.mx"
                }
            };
        }

        [Test]
        public void ValidatePlayerProfile_ValidPlayer_ReturnsTrue()
        {
            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePlayerProfile_NullPlayer_ReturnsFalse()
        {
            Player player = null;

            bool result = PlayerValidator.ValidatePlayerProfile(player);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_NullUser_ReturnsFalse()
        {
            _validPlayer.User = null;

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePlayerProfile_InvalidEmail_ReturnsFalse(string email)
        {
            _validPlayer.User.email = email;

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_EmailTooLong_ReturnsFalse()
        {
            _validPlayer.User.email = new string('a', 31);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ValidatePlayerProfile_InvalidUsername_ReturnsFalse(string username)
        {
            _validPlayer.username = username;

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_UsernameTooLong_ReturnsFalse()
        {
            _validPlayer.username = new string('a', 21);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_NameTooLong_ReturnsFalse()
        {
            _validPlayer.name = new string('a', 21);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_LastNameTooLong_ReturnsFalse()
        {
            _validPlayer.lastName = new string('a', 31);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_FacebookUsernameTooLong_ReturnsFalse()
        {
            _validPlayer.facebookUsername = new string('a', 31);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_InstagramUsernameTooLong_ReturnsFalse()
        {
            _validPlayer.instagramUsername = new string('a', 31);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_DiscordUsernameTooLong_ReturnsFalse()
        {
            _validPlayer.discordUsername = new string('a', 31);

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidatePlayerProfile_OptionalFieldsNull_ReturnsTrue()
        {
            _validPlayer.name = null;
            _validPlayer.lastName = null;
            _validPlayer.facebookUsername = null;
            _validPlayer.instagramUsername = null;
            _validPlayer.discordUsername = null;

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidatePlayerProfile_OptionalFieldsEmpty_ReturnsTrue()
        {
            _validPlayer.name = "";
            _validPlayer.lastName = "";
            _validPlayer.facebookUsername = "";
            _validPlayer.instagramUsername = "";
            _validPlayer.discordUsername = "";

            bool result = PlayerValidator.ValidatePlayerProfile(_validPlayer);

            Assert.That(result, Is.True);
        }
    }
}
