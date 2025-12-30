using DataAccess.Test;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class PlayerDAOTest
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

        #region GetPlayerByUserID

        [Test]
        public void GetPlayerByUserID_UserExists_ReturnsPlayer()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var player = new Player { playerID = Guid.NewGuid(), userID = userId, User = new User { userID = userId } };
            _playersData.Add(player);

            // Act
            var result = _playerDAO.GetPlayerByUserID(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.playerID, Is.EqualTo(player.playerID));
        }

        [Test]
        public void GetPlayerByUserID_UserDoesNotExist_ReturnsNull()
        {
            // Arrange
            Guid userId = Guid.NewGuid();

            // Act
            var result = _playerDAO.GetPlayerByUserID(userId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPlayerByUserID_DbError_ReturnsNull()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            _context.Setup(c => c.Players).Throws(new DbUpdateException()); // Simulate DB failure accessing Players

            // Act
            var result = _playerDAO.GetPlayerByUserID(userId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetPlayerById

        [Test]
        public void GetPlayerById_PlayerExists_ReturnsPlayer()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var player = new Player { playerID = playerId, User = new User() };
            _playersData.Add(player);

            // Act
            var result = _playerDAO.GetPlayerById(playerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.playerID, Is.EqualTo(playerId));
        }

        [Test]
        public void GetPlayerById_PlayerDoesNotExist_ReturnsNull()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act
            var result = _playerDAO.GetPlayerById(playerId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPlayerById_EmptyId_ReturnsNull()
        {
            // Act
            var result = _playerDAO.GetPlayerById(Guid.Empty);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetEmailByPlayerID

        [Test]
        public void GetEmailByPlayerID_PlayerExists_ReturnsEmail()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            string email = "test@example.com";
            var player = new Player { playerID = playerId, User = new User { email = email } };
            _playersData.Add(player);

            // Act
            var result = _playerDAO.GetEmailByPlayerID(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(email));
        }

        [Test]
        public void GetEmailByPlayerID_PlayerDoesNotExist_ReturnsEmptyString()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act
            var result = _playerDAO.GetEmailByPlayerID(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetEmailByPlayerID_EmptyId_ReturnsEmptyString()
        {
            // Act
            var result = _playerDAO.GetEmailByPlayerID(Guid.Empty);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        #endregion

        #region ValidateEmailNotDuplicated

        [Test]
        public void ValidateEmailNotDuplicated_EmailUnique_ReturnsTrue()
        {
            // Arrange
            string email = "unique@example.com";

            // Act
            bool result = _playerDAO.ValidateEmailNotDuplicated(email);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateEmailNotDuplicated_EmailExists_ReturnsFalse()
        {
            // Arrange
            string email = "duplicate@example.com";
            _usersData.Add(new User { email = email });

            // Act
            bool result = _playerDAO.ValidateEmailNotDuplicated(email);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateEmailNotDuplicated_DbError_ReturnsFalse()
        {
            // Arrange
            _context.Setup(c => c.Users).Throws(new InvalidOperationException()); // General exception matches catch block

            // Act
            bool result = _playerDAO.ValidateEmailNotDuplicated("any@email.com");

            // Assert
            Assert.That(result, Is.False); // Code returns false on exception
        }

        #endregion

        #region ValidateUsernameNotDuplicated

        [Test]
        public void ValidateUsernameNotDuplicated_UsernameUnique_ReturnsTrue()
        {
            // Arrange
            string username = "UniqueUser";

            // Act
            bool result = _playerDAO.ValidateUsernameNotDuplicated(username);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateUsernameNotDuplicated_UsernameExists_ReturnsFalse()
        {
            // Arrange
            string username = "DuplicateUser";
            _playersData.Add(new Player { username = username });

            // Act
            bool result = _playerDAO.ValidateUsernameNotDuplicated(username);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region VerifyIsPlayerGuest

        [Test]
        public void VerifyIsPlayerGuest_PlayerExists_ReturnsFalse()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            _playersData.Add(new Player { playerID = playerId });

            // Act
            bool result = _playerDAO.VerifyIsPlayerGuest(playerId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyIsPlayerGuest_PlayerDoesNotExist_ReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act
            bool result = _playerDAO.VerifyIsPlayerGuest(playerId);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion
    }
}