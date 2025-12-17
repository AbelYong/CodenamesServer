using DataAccess.DataRequests;
using DataAccess.Test;
using DataAccess.Tests.Util;
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
    public class FriendDAOTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Friendship>> _friendshipSet;
        private Mock<DbSet<Player>> _playerSet;
        private FriendDAO _friendDAO;
        private List<Friendship> _friendshipsData;
        private List<Player> _playersData;

        [SetUp]
        public void Setup()
        {
            _friendshipsData = new List<Friendship>();
            _playersData = new List<Player>();

            _friendshipSet = TestUtil.GetQueryableMockDbSet(_friendshipsData);
            _playerSet = TestUtil.GetQueryableMockDbSet(_playersData);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Friendships).Returns(_friendshipSet.Object);
            _context.Setup(c => c.Players).Returns(_playerSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _friendDAO = new FriendDAO(_contextFactory.Object);
        }

        #region SearchPlayers

        [Test]
        public void SearchPlayers_ValidQuery_ReturnsMatchingPlayersExcludingSelfAndFriends()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            Guid strangerId = Guid.NewGuid();

            _playersData.Add(new Player { playerID = meId, username = "Me", User = new User() });
            _playersData.Add(new Player { playerID = friendId, username = "FriendTarget", User = new User() });
            _playersData.Add(new Player { playerID = strangerId, username = "StrangerTarget", User = new User() });

            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendId, requestStatus = true });

            // Act
            var result = _friendDAO.SearchPlayers("target", meId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().playerID, Is.EqualTo(strangerId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SearchPlayers_InvalidQuery_ReturnsEmptyList(string query)
        {
            // Act
            var result = _friendDAO.SearchPlayers(query, Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_SqlException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(SqlExceptionCreator.Create());

            // Act
            var result = _friendDAO.SearchPlayers("query", Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_EntityException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            // Act
            var result = _friendDAO.SearchPlayers("query", Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_TimeoutException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new TimeoutException());

            // Act
            var result = _friendDAO.SearchPlayers("query", Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_GeneralException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new Exception());

            // Act
            var result = _friendDAO.SearchPlayers("query", Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetFriends

        [Test]
        public void GetFriends_PlayerHasFriends_ReturnsFriendList()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid friendA = Guid.NewGuid();
            Guid friendB = Guid.NewGuid();

            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendA, requestStatus = true });
            _friendshipsData.Add(new Friendship { playerID = friendB, friendID = meId, requestStatus = true });

            _playersData.Add(new Player { playerID = friendA, User = new User() });
            _playersData.Add(new Player { playerID = friendB, User = new User() });

            // Act
            var result = _friendDAO.GetFriends(meId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(p => p.playerID == friendA), Is.True);
            Assert.That(result.Any(p => p.playerID == friendB), Is.True);
        }

        [Test]
        public void GetFriends_EmptyId_ReturnsEmptyList()
        {
            // Act
            var result = _friendDAO.GetFriends(Guid.Empty);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFriends_DbException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            // Act
            var result = _friendDAO.GetFriends(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetIncomingRequests

        [Test]
        public void GetIncomingRequests_RequestsExist_ReturnsRequesters()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();

            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });
            _playersData.Add(new Player { playerID = requesterId, User = new User() });

            // Act
            var result = _friendDAO.GetIncomingRequests(meId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().playerID, Is.EqualTo(requesterId));
        }

        [Test]
        public void GetIncomingRequests_EmptyId_ReturnsEmptyList()
        {
            // Act
            var result = _friendDAO.GetIncomingRequests(Guid.Empty);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetIncomingRequests_DbException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            // Act
            var result = _friendDAO.GetIncomingRequests(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetSentRequests

        [Test]
        public void GetSentRequests_RequestsExist_ReturnsTargets()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();

            _friendshipsData.Add(new Friendship { playerID = meId, friendID = targetId, requestStatus = false });
            _playersData.Add(new Player { playerID = targetId, User = new User() });

            // Act
            var result = _friendDAO.GetSentRequests(meId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().playerID, Is.EqualTo(targetId));
        }

        [Test]
        public void GetSentRequests_EmptyId_ReturnsEmptyList()
        {
            // Act
            var result = _friendDAO.GetSentRequests(Guid.Empty);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetSentRequests_DbException_ReturnsEmptyList()
        {
            // Arrange
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            // Act
            var result = _friendDAO.GetSentRequests(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region SendFriendRequest

        [Test]
        public void SendFriendRequest_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();

            // Act
            var result = _friendDAO.SendFriendRequest(fromId, toId);

            // Assert
            Assert.That(result.Success, Is.True);
            _friendshipSet.Verify(m => m.Add(It.Is<Friendship>(f => f.playerID == fromId && f.friendID == toId && !f.requestStatus)), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void SendFriendRequest_MissingIds_ReturnsMissingDataError()
        {
            // Act
            var result = _friendDAO.SendFriendRequest(Guid.Empty, Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.MISSING_DATA));
        }

        [Test]
        public void SendFriendRequest_SelfRequest_ReturnsInvalidDataError()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            var result = _friendDAO.SendFriendRequest(id, id);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
        }

        [Test]
        public void SendFriendRequest_DuplicateRequest_ReturnsDuplicateError()
        {
            // Arrange
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = fromId, friendID = toId });

            // Act
            var result = _friendDAO.SendFriendRequest(fromId, toId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
        }

        [Test]
        public void SendFriendRequest_DbUpdateException_ReturnsDbError()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act
            var result = _friendDAO.SendFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SendFriendRequest_EntityException_ReturnsDbError()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            // Act
            var result = _friendDAO.SendFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        #endregion

        #region AcceptFriendRequest

        [Test]
        public void AcceptFriendRequest_RequestExists_UpdatesStatusAndReturnsSuccess()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            // Act
            var result = _friendDAO.AcceptFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(friendship.requestStatus, Is.True);
            _friendshipSet.Verify(m => m.Add(It.Is<Friendship>(f => f.playerID == meId && f.friendID == requesterId && f.requestStatus)), Times.Once); // Reciprocal
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void AcceptFriendRequest_RequestNotFound_ReturnsNotFoundError()
        {
            // Act
            var result = _friendDAO.AcceptFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void AcceptFriendRequest_DbException_ReturnsDbError()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });

            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act
            var result = _friendDAO.AcceptFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        #endregion

        #region RejectFriendRequest

        [Test]
        public void RejectFriendRequest_RequestExists_RemovesRequestAndReturnsSuccess()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            // Act
            var result = _friendDAO.RejectFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.Success, Is.True);
            _friendshipSet.Verify(m => m.Remove(friendship), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void RejectFriendRequest_RequestNotFound_ReturnsNotFoundError()
        {
            // Act
            var result = _friendDAO.RejectFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void RejectFriendRequest_DbException_ReturnsDbError()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });

            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            // Act
            var result = _friendDAO.RejectFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        #endregion

        #region RemoveFriend

        [Test]
        public void RemoveFriend_FriendshipExists_RemovesBothLinksAndReturnsSuccess()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            var link1 = new Friendship { playerID = meId, friendID = friendId, requestStatus = true };
            var link2 = new Friendship { playerID = friendId, friendID = meId, requestStatus = true };
            _friendshipsData.Add(link1);
            _friendshipsData.Add(link2);

            // Act
            var result = _friendDAO.RemoveFriend(meId, friendId);

            // Assert
            Assert.That(result.Success, Is.True);
            _friendshipSet.Verify(m => m.RemoveRange(It.Is<IEnumerable<Friendship>>(l => l.Count() == 2)), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void RemoveFriend_FriendshipNotFound_ReturnsNotFoundError()
        {
            // Act
            var result = _friendDAO.RemoveFriend(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void RemoveFriend_DbException_ReturnsDbError()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendId, requestStatus = true });

            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act
            var result = _friendDAO.RemoveFriend(meId, friendId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        #endregion
    }
}