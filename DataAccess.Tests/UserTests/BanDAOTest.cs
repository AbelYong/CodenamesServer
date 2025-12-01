using DataAccess.Test;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class BanDAOTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Ban>> _banSet;
        private BanDAO _banDAO;
        private List<Ban> _bansData;

        [SetUp]
        public void Setup()
        {
            _bansData = new List<Ban>();
            _banSet = TestUtil.GetQueryableMockDbSet(_bansData);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Bans).Returns(_banSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _banDAO = new BanDAO(_contextFactory.Object);
        }

        #region GetActiveBan

        [Test]
        public void GetActiveBan_UserHasActiveBan_ReturnsBan()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var activeBan = new Ban
            {
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(1)
            };
            _bansData.Add(activeBan);

            // Act
            var result = _banDAO.GetActiveBan(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(activeBan));
        }

        [Test]
        public void GetActiveBan_UserHasExpiredBan_ReturnsNull()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var expiredBan = new Ban
            {
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(-1)
            };
            _bansData.Add(expiredBan);

            // Act
            var result = _banDAO.GetActiveBan(userId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_UserHasMultipleActiveBans_ReturnsLatestTimeout()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var shortBan = new Ban
            {
                banID = Guid.NewGuid(),
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(1)
            };
            var longBan = new Ban
            {
                banID = Guid.NewGuid(),
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(24)
            };

            _bansData.Add(shortBan);
            _bansData.Add(longBan);

            // Act
            var result = _banDAO.GetActiveBan(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.banID, Is.EqualTo(longBan.banID));
        }

        [Test]
        public void GetActiveBan_UserHasNoBans_ReturnsNull()
        {
            // Arrange
            // Lista vacía

            // Act
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_SqlException_ReturnsNullAndLogs()
        {
            // Arrange
            _context.Setup(c => c.Bans).Throws(SqlExceptionCreator.Create());

            // Act
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_EntityException_ReturnsNullAndLogs()
        {
            // Arrange
            _context.Setup(c => c.Bans).Throws(new EntityException());

            // Act
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_TimeoutException_ReturnsNullAndLogs()
        {
            // Arrange
            _context.Setup(c => c.Bans).Throws(new TimeoutException());

            // Act
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_GeneralException_ReturnsNullAndLogs()
        {
            // Arrange
            _context.Setup(c => c.Bans).Throws(new Exception());

            // Act
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ApplyBan

        [Test]
        public void ApplyBan_ValidBan_AddsAndSaves()
        {
            // Arrange
            var ban = new Ban { banID = Guid.NewGuid() };

            // Act
            _banDAO.ApplyBan(ban);

            // Assert
            _banSet.Verify(m => m.Add(ban), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void ApplyBan_DbUpdateException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act & Assert
            Assert.Throws<DbUpdateException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_SqlException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(SqlExceptionCreator.Create());

            // Act & Assert
            Assert.Throws<SqlException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_EntityException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            // Act & Assert
            Assert.Throws<EntityException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_TimeoutException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new TimeoutException());

            // Act & Assert
            Assert.Throws<TimeoutException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_GeneralException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new Exception());

            // Act & Assert
            Assert.Throws<Exception>(() => _banDAO.ApplyBan(new Ban()));
        }

        #endregion
    }
}