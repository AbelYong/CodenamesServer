using DataAccess.DataRequests;
using DataAccess.Tests.Util;
using DataAccess.Users;
using DataAccess.Util;
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
        private PlayerRepository _playerRepository;
        private List<Player> _playersData;
        private List<User> _usersData;

        [SetUp]
        public void Setup()
        {
            _playersData = new List<Player>();
            _usersData = new List<User>();

            _playerSet = TestUtil.GetQueryableMockDbSet(_playersData);
            _userSet = TestUtil.GetQueryableMockDbSet(_usersData);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Players).Returns(_playerSet.Object);
            _context.Setup(c => c.Users).Returns(_userSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerRepository = new PlayerRepository(_contextFactory.Object);
        }

        [Test]
        public void UpdateProfile_ValidUpdate_UpdatesSavesAndReturnsSuccess()
        {
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string newEmail = "new@gmail.com";
            string oldUser = "OldUser";
            string newUser = "NewUser";

            var dbUser = new User { userID = userId, email = oldEmail };
            _usersData.Add(dbUser);
            var dbPlayer = new Player { playerID = playerId, userID = userId, username = oldUser };
            dbPlayer.User = dbUser;
            _playersData.Add(dbPlayer);
            var playerUpdate = CreateValidPlayer(userId, playerId, newUser, newEmail);

            var result = _playerRepository.UpdateProfile(playerUpdate);
            Player afterUpdate = _playerRepository.GetPlayerByUserID(userId);

            Assert.That(result.Success && VerifyEqualPlayers(playerUpdate, afterUpdate));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        private static bool VerifyEqualPlayers(Player playerUpdate, Player afterUpdate)
        {
            return
                playerUpdate.playerID.Equals(afterUpdate.playerID) &&
                playerUpdate.username.Equals(afterUpdate.username) &&
                playerUpdate.User.userID.Equals(afterUpdate.User.userID) &&
                playerUpdate.User.email.Equals(afterUpdate.User.email);
        }

        [Test]
        public void UpdateProfile_InvalidData_ReturnsInvalidDataError()
        {
            var player = new Player { username = "" };
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.INVALID_DATA
            };

            var result = _playerRepository.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_UserNotFound_ReturnsNotFoundError()
        {
            Guid userId = Guid.NewGuid();
            var player = CreateValidPlayer(userId, Guid.NewGuid(), "User", "test@email.com");
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.NOT_FOUND
            };

            var result = _playerRepository.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_EmailDuplicated_ReturnsDuplicateError()
        {
            Guid userId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string newEmail = "taken@gmail.com";
            _usersData.Add(new User { userID = userId, email = oldEmail });
            _usersData.Add(new User { userID = Guid.NewGuid(), email = newEmail });
            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", newEmail);
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.DUPLICATE
            };

            var result = _playerRepository.UpdateProfile(playerUpdate);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_InvalidEmail_ReturnsInvalidError()
        {
            Guid userId = Guid.NewGuid();
            string oldEmail = "old@gmail.com";
            string invalidEmail = "unallowed@domain.com";
            _usersData.Add(new User { userID = userId, email = oldEmail });
            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", invalidEmail);
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.INVALID_DATA
            };

            var result = _playerRepository.UpdateProfile(playerUpdate);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_PlayerNotFound_ReturnsNotFoundError()
        {
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            _usersData.Add(new User { userID = userId, email = "test@test.com" });
            var playerUpdate = CreateValidPlayer(userId, playerId, "User", "test@test.com");
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.NOT_FOUND
            };

            var result = _playerRepository.UpdateProfile(playerUpdate);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_UsernameDuplicated_ReturnsDuplicateError()
        {
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            string email = "test@test.com";
            string newUsername = "TakenName";
            _usersData.Add(new User { userID = userId, email = email });
            _playersData.Add(new Player { playerID = playerId, userID = userId, username = "OldName" });
            _playersData.Add(new Player { playerID = Guid.NewGuid(), username = newUsername });
            var playerUpdate = CreateValidPlayer(userId, playerId, newUsername, email);
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.DUPLICATE
            };

            var result = _playerRepository.UpdateProfile(playerUpdate);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_DbError_ReturnsDbError()
        {
            Guid userId = Guid.NewGuid();
            var playerUpdate = CreateValidPlayer(userId, Guid.NewGuid(), "User", "email@test.com");
            _context.Setup(c => c.Users).Throws(new DbUpdateException());
            OperationResult expected = new OperationResult
            {
                Success = false,
                ErrorType = ErrorType.DB_ERROR
            };

            var result = _playerRepository.UpdateProfile(playerUpdate);

            Assert.That(result.Equals(expected));
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
