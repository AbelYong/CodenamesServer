using DataAccess.DataRequests;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace DataAccess.Test.UserTests
{
    [TestFixture]
    public class ProfileUpdateTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Player>> _playerSet;
        private Mock<DbSet<User>> _userSet;
        private PlayerDAO _playerDAO;
        private List<Player> _playersData;
        private List<User> _usersData;

        [SetUp]
        public void Setup()
        {
            // 1. Setup Data
            _playersData = new List<Player>();
            _usersData = new List<User>();

            _playerSet = TestUtil.GetQueryableMockDbSet(_playersData);
            _userSet = TestUtil.GetQueryableMockDbSet(_usersData);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Players).Returns(_playerSet.Object);
            _context.Setup(c => c.Users).Returns(_userSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerDAO = new PlayerDAO(_contextFactory.Object);
        }

        [Test]
        public void UpdateProfile_InvalidData_ReturnsInvalidDataError()
        {
            // Arrange
            var player = new Player { username = "" }; // Invalid: empty username

            // Act
            var result = _playerDAO.UpdateProfile(player);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
        }

        [Test]
        public void UpdateProfile_UserNotFound_ReturnsNotFoundError()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var player = CreateValidPlayer(userId, Guid.NewGuid(), "User", "test@email.com");
            // Don't add user to _usersData

            // Act
            var result = _playerDAO.UpdateProfile(player);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void UpdateProfile_EmailDuplicated_ReturnsDuplicateError()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string newEmail = "taken@gmail.com";

            // Existing user (me)
            _usersData.Add(new User { userID = userId, email = oldEmail });
            // Another user taking the email
            _usersData.Add(new User { userID = Guid.NewGuid(), email = newEmail });

            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", newEmail);

            // Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
        }

        [Test]
        public void UpdateProfile_InvalidEmail_ReturnsInvalidError()
        {
            //Arrange
            Guid userId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string invalidEmail = "unallowed@domain.com";

            _usersData.Add(new User { userID = userId, email = oldEmail });

            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", invalidEmail);

            //Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            //Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
        }

        [Test]
        public void UpdateProfile_PlayerNotFound_ReturnsNotFoundError()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();

            _usersData.Add(new User { userID = userId, email = "test@test.com" });
            // Don't add player to _playersData

            var playerUpdate = CreateValidPlayer(userId, playerId, "User", "test@test.com");

            // Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void UpdateProfile_UsernameDuplicated_ReturnsDuplicateError()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            string email = "test@test.com";
            string newUsername = "TakenName";

            _usersData.Add(new User { userID = userId, email = email });
            // Me
            _playersData.Add(new Player { playerID = playerId, userID = userId, username = "OldName" });
            // Other guy
            _playersData.Add(new Player { playerID = Guid.NewGuid(), username = newUsername });

            var playerUpdate = CreateValidPlayer(userId, playerId, newUsername, email);

            // Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
        }

        [Test]
        public void UpdateProfile_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string newEmail = "new@gmail.com";
            string oldUser = "OldUser";
            string newUser = "NewUser";

            // Setup DB state
            var dbUser = new User { userID = userId, email = oldEmail };
            _usersData.Add(dbUser);

            var dbPlayer = new Player { playerID = playerId, userID = userId, username = oldUser };
            _playersData.Add(dbPlayer);

            var playerUpdate = CreateValidPlayer(userId, playerId, newUser, newEmail);
            playerUpdate.name = "New Name";

            // Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            // Assert
            Assert.That(result.Success, Is.True);
            _context.Verify(c => c.SaveChanges(), Times.Once);

            // Verify changes reflected in referenced objects
            Assert.That(dbUser.email, Is.EqualTo(newEmail));
            Assert.That(dbPlayer.username, Is.EqualTo(newUser));
            Assert.That(dbPlayer.name, Is.EqualTo("New Name"));
        }

        [Test]
        public void UpdateProfile_DbError_ReturnsDbError()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", "email@test.com");

            // Force exception immediately on first DB access
            _context.Setup(c => c.Users).Throws(new DbUpdateException());

            // Act
            var result = _playerDAO.UpdateProfile(playerUpdate);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        private Player CreateValidPlayer(Guid userId, Guid playerId, string username, string email)
        {
            return new Player
            {
                playerID = playerId,
                userID = userId,
                username = username,
                User = new User { userID = userId, email = email },
                name = "ValidName",
                lastName = "ValidLast",
                facebookUsername = "ValidFB",
                instagramUsername = "ValidInsta",
                discordUsername = "ValidDiscord"
            };
        }
    }
}
