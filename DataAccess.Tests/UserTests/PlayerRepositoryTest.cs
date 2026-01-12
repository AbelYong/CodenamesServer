using DataAccess.Tests.Util;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class PlayerRepositoryTest
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
        public void GetPlayerByUserID_UserExists_ReturnsPlayer()
        {
            Guid userId = Guid.NewGuid();
            var player = new Player { playerID = Guid.NewGuid(), userID = userId, User = new User { userID = userId } };
            _playersData.Add(player);

            var result = _playerRepository.GetPlayerByUserID(userId);

            Assert.That(result.playerID.Equals(player.playerID));
        }

        [Test]
        public void GetPlayerByUserID_UserDoesNotExist_ReturnsNull()
        {
            Guid userId = Guid.NewGuid();

            var result = _playerRepository.GetPlayerByUserID(userId);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPlayerByUserID_DbError_ReturnsNull()
        {
            Guid userId = Guid.NewGuid();
            _context.Setup(c => c.Players).Throws(new DbUpdateException());

            var result = _playerRepository.GetPlayerByUserID(userId);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPlayerById_PlayerExists_ReturnsPlayer()
        {
            Guid playerId = Guid.NewGuid();
            var player = new Player { playerID = playerId, User = new User() };
            _playersData.Add(player);

            var result = _playerRepository.GetPlayerById(playerId);

            Assert.That(result.playerID.Equals(playerId));
        }

        [Test]
        public void GetPlayerById_PlayerDoesNotExist_ReturnsNull()
        {
            Guid playerId = Guid.NewGuid();

            var result = _playerRepository.GetPlayerById(playerId);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPlayerById_EmptyId_ReturnsNull()
        {
            var result = _playerRepository.GetPlayerById(Guid.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEmailByPlayerID_PlayerExists_ReturnsEmail()
        {
            Guid playerId = Guid.NewGuid();
            string email = "test@example.com";
            var player = new Player { playerID = playerId, User = new User { email = email } };
            _playersData.Add(player);

            var result = _playerRepository.GetEmailByPlayerID(playerId);

            Assert.That(result.Equals(email));
        }

        [Test]
        public void GetEmailByPlayerID_PlayerDoesNotExist_ReturnsEmptyString()
        {
            Guid playerId = Guid.NewGuid();

            var result = _playerRepository.GetEmailByPlayerID(playerId);

            Assert.That(result.Equals(string.Empty));
        }

        [Test]
        public void GetEmailByPlayerID_EmptyId_ReturnsEmptyString()
        {
            var result = _playerRepository.GetEmailByPlayerID(Guid.Empty);

            Assert.That(result.Equals(string.Empty));
        }

        [Test]
        public void ValidateEmailNotDuplicated_EmailUnique_ReturnsTrue()
        {
            string email = "unique@example.com";

            bool result = _playerRepository.ValidateEmailNotDuplicated(email);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateEmailNotDuplicated_EmailExists_ReturnsFalse()
        {
            string email = "duplicate@example.com";
            _usersData.Add(new User { email = email });

            bool result = _playerRepository.ValidateEmailNotDuplicated(email);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateUsernameNotDuplicated_UsernameUnique_ReturnsTrue()
        {
            string username = "UniqueUser";

            bool result = _playerRepository.ValidateUsernameNotDuplicated(username);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateUsernameNotDuplicated_UsernameExists_ReturnsFalse()
        {
            string username = "DuplicateUser";
            _playersData.Add(new Player { username = username });

            bool result = _playerRepository.ValidateUsernameNotDuplicated(username);

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyIsPlayerGuest_PlayerExists_ReturnsFalse()
        {
            Guid playerId = Guid.NewGuid();
            _playersData.Add(new Player { playerID = playerId });

            bool result = _playerRepository.VerifyIsPlayerGuest(playerId);

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyIsPlayerGuest_PlayerDoesNotExist_ReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();

            bool result = _playerRepository.VerifyIsPlayerGuest(playerId);

            Assert.That(result, Is.True);
        }
    }
}