using DataAccess.Scoreboards;
using DataAccess.Users;
using DataAccess.Tests.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace DataAccess.Test.ScoreboardTests
{
    [TestFixture]
    public class ScoreboardDAOTests
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<IPlayerDAO> _playerDAO;
        private Mock<DbSet<Scoreboard>> _scoreboardSet;
        private ScoreboardDAO _scoreboardDAO;
        private List<Scoreboard> _data;

        [SetUp]
        public void Setup()
        {
            _data = new List<Scoreboard>();
            _scoreboardSet = TestUtil.GetQueryableMockDbSet(_data);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Scoreboards).Returns(_scoreboardSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerDAO = new Mock<IPlayerDAO>();

            _scoreboardDAO = new ScoreboardDAO(_contextFactory.Object, _playerDAO.Object);
        }

        [Test]
        public void UpdateMatchesWon_PlayerIsGuest_ReturnsTrueDatabaseNotChanged()
        {
            Guid guestId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            bool result = _scoreboardDAO.UpdateMatchesWon(guestId);

            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateMatchesWon_PlayerExists_ScoreboardExists_IncrementsCountSavesAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            var existingScoreboard = new Scoreboard { playerID = playerId, mostGamesWon = 5 };
            _data.Add(existingScoreboard);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            Assert.That(result && existingScoreboard.mostGamesWon.Equals(6));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateMatchesWon_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            Assert.That(result);
            _scoreboardSet.Verify(s => s.Add(It.Is<Scoreboard>(sb =>
                sb.playerID == playerId &&
                sb.mostGamesWon == 1
            )), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateMatchesWon_DatabaseError_ReturnsFalse()
        {
            Guid playerId = Guid.NewGuid();
            _data.Add(new Scoreboard { playerID = playerId });
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerIsGuest_ReturnsTrueDatabaseNotChanged()
        {
            Guid guestId = Guid.NewGuid();
            TimeSpan time = TimeSpan.FromMinutes(5);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            bool result = _scoreboardDAO.UpdateFastestMatchRecord(guestId, time);

            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardExists_NewRecordIsFaster_UpdatesAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            TimeSpan oldRecord = TimeSpan.FromMinutes(10);
            TimeSpan newRecord = TimeSpan.FromMinutes(5);
            var existingScoreboard = new Scoreboard { playerID = playerId, fastestGame = oldRecord };
            _data.Add(existingScoreboard);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            Assert.That(result && existingScoreboard.fastestGame.Equals(newRecord));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardExists_NewRecordIsSlower_DoesNotUpdateAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            TimeSpan oldRecord = TimeSpan.FromMinutes(5);
            TimeSpan newRecord = TimeSpan.FromMinutes(10);

            var existingScoreboard = new Scoreboard { playerID = playerId, fastestGame = oldRecord };
            _data.Add(existingScoreboard);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            Assert.That(result && existingScoreboard.fastestGame.Equals(oldRecord));
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardSavesAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            TimeSpan newRecord = TimeSpan.FromMinutes(5);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            _scoreboardSet.Verify(s => s.Add(It.Is<Scoreboard>(sb =>
                sb.playerID == playerId &&
                sb.fastestGame == newRecord
            )), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
            Assert.That(result);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerIsGuest_ReturnsTrue()
        {
            Guid guestId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            bool result = _scoreboardDAO.UpdateAssassinsPicked(guestId);

            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerExists_ScoreboardExists_IncrementsCountSavesAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            var existingScoreboard = new Scoreboard { playerID = playerId, assassinsRevealed = 2 };
            _data.Add(existingScoreboard);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateAssassinsPicked(playerId);

            Assert.That(result && existingScoreboard.assassinsRevealed.Equals(3));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardSavesAndReturnsTrue()
        {
            Guid playerId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            bool result = _scoreboardDAO.UpdateAssassinsPicked(playerId);

            Assert.That(result);
            _scoreboardSet.Verify(s => s.Add(It.Is<Scoreboard>(sb =>
                sb.playerID == playerId &&
                sb.assassinsRevealed == 1
            )), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }
    }
}
