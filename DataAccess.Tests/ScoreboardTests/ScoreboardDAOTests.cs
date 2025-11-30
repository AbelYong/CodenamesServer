using DataAccess.Scoreboards;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

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
        public void UpdateMatchesWon_PlayerIsGuest_ReturnsTrue()
        {
            // Arrange
            Guid guestId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            // Act
            bool result = _scoreboardDAO.UpdateMatchesWon(guestId);

            // Assert
            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never); // Should not touch DB
        }

        [Test]
        public void UpdateMatchesWon_PlayerExists_ScoreboardExists_IncrementsCountAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var existingScoreboard = new Scoreboard { playerID = playerId, mostGamesWon = 5 };
            _data.Add(existingScoreboard);

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            // Assert
            Assert.That(result);
            Assert.That(existingScoreboard.mostGamesWon, Is.EqualTo(6));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateMatchesWon_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            // Data list is empty

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            // Assert
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
            // Arrange
            Guid playerId = Guid.NewGuid();
            _data.Add(new Scoreboard { playerID = playerId });
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Simulate DB Error on SaveChanges
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act
            bool result = _scoreboardDAO.UpdateMatchesWon(playerId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerIsGuest_ReturnsTrue()
        {
            // Arrange
            Guid guestId = Guid.NewGuid();
            TimeSpan time = TimeSpan.FromMinutes(5);
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            // Act
            bool result = _scoreboardDAO.UpdateFastestMatchRecord(guestId, time);

            // Assert
            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardExists_NewRecordIsFaster_UpdatesAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            TimeSpan oldRecord = TimeSpan.FromMinutes(10);
            TimeSpan newRecord = TimeSpan.FromMinutes(5);

            var existingScoreboard = new Scoreboard { playerID = playerId, fastestGame = oldRecord };
            _data.Add(existingScoreboard);

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            // Assert
            Assert.That(result);
            Assert.That(existingScoreboard.fastestGame, Is.EqualTo(newRecord));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardExists_NewRecordIsSlower_DoesNotUpdateAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            TimeSpan oldRecord = TimeSpan.FromMinutes(5);
            TimeSpan newRecord = TimeSpan.FromMinutes(10); // Slower

            var existingScoreboard = new Scoreboard { playerID = playerId, fastestGame = oldRecord };
            _data.Add(existingScoreboard);

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            // Assert
            Assert.That(result);
            Assert.That(existingScoreboard.fastestGame, Is.EqualTo(oldRecord)); // Should remain unchanged
            _context.Verify(c => c.SaveChanges(), Times.Once); // Code saves regardless
        }

        [Test]
        public void UpdateFastestMatchRecord_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            TimeSpan newRecord = TimeSpan.FromMinutes(5);

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateFastestMatchRecord(playerId, newRecord);

            // Assert
            Assert.That(result);
            _scoreboardSet.Verify(s => s.Add(It.Is<Scoreboard>(sb =>
                sb.playerID == playerId &&
                sb.fastestGame == newRecord
            )), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerIsGuest_ReturnsTrue()
        {
            // Arrange
            Guid guestId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(guestId)).Returns(true);

            // Act
            bool result = _scoreboardDAO.UpdateAssassinsPicked(guestId);

            // Assert
            Assert.That(result);
            _context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerExists_ScoreboardExists_IncrementsCountAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var existingScoreboard = new Scoreboard { playerID = playerId, assassinsRevealed = 2 };
            _data.Add(existingScoreboard);

            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateAssassinsPicked(playerId);

            // Assert
            Assert.That(result);
            Assert.That(existingScoreboard.assassinsRevealed, Is.EqualTo(3));
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void UpdateAssassinsPicked_PlayerExists_ScoreboardDoesNotExist_CreatesScoreboardAndReturnsTrue()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            _playerDAO.Setup(p => p.VerifyIsPlayerGuest(playerId)).Returns(false);

            // Act
            bool result = _scoreboardDAO.UpdateAssassinsPicked(playerId);

            // Assert
            Assert.That(result);
            _scoreboardSet.Verify(s => s.Add(It.Is<Scoreboard>(sb =>
                sb.playerID == playerId &&
                sb.assassinsRevealed == 1
            )), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }
    }
}
