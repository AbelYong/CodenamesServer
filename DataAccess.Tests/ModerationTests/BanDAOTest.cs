using DataAccess.Moderation;
using DataAccess.Tests.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace DataAccess.Tests.ModerationTests
{
    [TestFixture]
    public class BanDAOTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Ban>> _banSet;
        private BanRepository _banDAO;
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

            _banDAO = new BanRepository(_contextFactory.Object);
        }

        [Test]
        public void GetActiveBan_UserHasActiveBan_ReturnsBan()
        {
            Guid userId = Guid.NewGuid();
            var activeBan = new Ban
            {
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(1)
            };
            _bansData.Add(activeBan);

            var result = _banDAO.GetActiveBan(userId);

            Assert.That(result, Is.EqualTo(activeBan));
        }

        [Test]
        public void GetActiveBan_UserHasExpiredBan_ReturnsNull()
        {
            Guid userId = Guid.NewGuid();
            var expiredBan = new Ban
            {
                userID = userId,
                timeout = DateTimeOffset.Now.AddHours(-1)
            };
            _bansData.Add(expiredBan);

            var result = _banDAO.GetActiveBan(userId);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_UserHasMultipleActiveBans_ReturnsLatestTimeout()
        {
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

            var result = _banDAO.GetActiveBan(userId);

            Assert.That(result.banID, Is.EqualTo(longBan.banID));
        }

        [Test]
        public void GetActiveBan_UserHasNoBans_ReturnsNull()
        {
            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_SqlException_ReturnsNull()
        {
            _context.Setup(c => c.Bans).Throws(SqlExceptionCreator.Create());

            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_EntityException_ReturnsNull()
        {
            _context.Setup(c => c.Bans).Throws(new EntityException());

            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_TimeoutException_ReturnsNull()
        {
            _context.Setup(c => c.Bans).Throws(new TimeoutException());

            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetActiveBan_GeneralException_ReturnsNull()
        {
            _context.Setup(c => c.Bans).Throws(new Exception());

            var result = _banDAO.GetActiveBan(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ApplyBan_ValidBan_AddsAndSaves()
        {
            var ban = new Ban { banID = Guid.NewGuid() };

            _banDAO.ApplyBan(ban);

            _banSet.Verify(m => m.Add(ban), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void ApplyBan_DbUpdateException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            Assert.Throws<DbUpdateException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_SqlException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(SqlExceptionCreator.Create());

            Assert.Throws<SqlException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_EntityException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            Assert.Throws<EntityException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_TimeoutException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new TimeoutException());

            Assert.Throws<TimeoutException>(() => _banDAO.ApplyBan(new Ban()));
        }

        [Test]
        public void ApplyBan_GeneralException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new Exception());

            Assert.Throws<Exception>(() => _banDAO.ApplyBan(new Ban()));
        }
    }
}