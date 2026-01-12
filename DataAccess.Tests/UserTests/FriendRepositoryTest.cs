using DataAccess.DataRequests;
using DataAccess.Tests.Util;
using DataAccess.Users;
using DataAccess.Util;
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
    public class FriendRepositoryTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Friendship>> _friendshipSet;
        private Mock<DbSet<Player>> _playerSet;
        private FriendRepository _friendRepository;
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

            _friendRepository = new FriendRepository(_contextFactory.Object);
        }

        [Test]
        public void SearchPlayers_ValidQuery_ReturnsMatchingPlayersExcludingSelfAndFriends()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            Guid strangerId = Guid.NewGuid();
            _playersData.Add(new Player { playerID = meId, username = "Me", User = new User() });
            _playersData.Add(new Player { playerID = friendId, username = "FriendTarget", User = new User() });
            _playersData.Add(new Player { playerID = strangerId, username = "StrangerTarget", User = new User() });
            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendId, requestStatus = true });

            var result = _friendRepository.SearchPlayers("target", meId);

            Assert.That(result.Players.First().playerID, Is.EqualTo(strangerId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SearchPlayers_InvalidQuery_ReturnsEmptyList(string query)
        {
            var result = _friendRepository.SearchPlayers(query, Guid.NewGuid());

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void SearchPlayers_SqlException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(SqlExceptionCreator.Create());

            var result = _friendRepository.SearchPlayers("query", Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SearchPlayers_EntityException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            var result = _friendRepository.SearchPlayers("query", Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SearchPlayers_GeneralException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(new Exception());

            var result = _friendRepository.SearchPlayers("query", Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void GetFriends_PlayerHasFriends_ReturnsFriendList()
        {
            Guid meId = Guid.NewGuid();
            Guid friendA = Guid.NewGuid();
            Guid friendB = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendA, requestStatus = true });
            _friendshipsData.Add(new Friendship { playerID = friendB, friendID = meId, requestStatus = true });
            _playersData.Add(new Player { playerID = friendA, User = new User() });
            _playersData.Add(new Player { playerID = friendB, User = new User() });

            var result = _friendRepository.GetFriends(meId);

            Assert.That(result.Players.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetFriends_EmptyId_ReturnsEmptyList()
        {
            var result = _friendRepository.GetFriends(Guid.Empty);

            Assert.That(result.Players, Is.Empty);
        }

        [Test]
        public void GetFriends_DbException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            var result = _friendRepository.GetFriends(Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void GetFriends_SqlException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(SqlExceptionCreator.Create());

            var result = _friendRepository.GetFriends(Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void GetIncomingRequests_RequestsExist_ReturnsRequesters()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });
            _playersData.Add(new Player { playerID = requesterId, User = new User() });

            var result = _friendRepository.GetIncomingRequests(meId);

            Assert.That(result.Players.First().playerID, Is.EqualTo(requesterId));
        }

        [Test]
        public void GetIncomingRequests_EmptyId_ReturnsEmptyList()
        {
            var result = _friendRepository.GetIncomingRequests(Guid.Empty);

            Assert.That(result.Players, Is.Empty);
        }

        [Test]
        public void GetIncomingRequests_DbException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            var result = _friendRepository.GetIncomingRequests(Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void GetSentRequests_RequestsExist_ReturnsTargets()
        {
            Guid meId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = meId, friendID = targetId, requestStatus = false });
            _playersData.Add(new Player { playerID = targetId, User = new User() });

            var result = _friendRepository.GetSentRequests(meId);

            Assert.That(result.Players.First().playerID, Is.EqualTo(targetId));
        }

        [Test]
        public void GetSentRequests_EmptyId_ReturnsEmptyList()
        {
            var result = _friendRepository.GetSentRequests(Guid.Empty);

            Assert.That(result.Players, Is.Empty);
        }

        [Test]
        public void GetSentRequests_DbException_ReturnsDbError()
        {
            _context.Setup(c => c.Friendships).Throws(new EntityException());

            var result = _friendRepository.GetSentRequests(Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SendFriendRequest_ValidRequest_ReturnsSuccess()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();

            var result = _friendRepository.SendFriendRequest(fromId, toId);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SendFriendRequest_ValidRequest_AddsFriendshipToContext()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();

            _friendRepository.SendFriendRequest(fromId, toId);

            _friendshipSet.Verify(m => m.Add(It.Is<Friendship>(f => f.playerID == fromId && f.friendID == toId && !f.requestStatus)), Times.Once);
        }

        [Test]
        public void SendFriendRequest_MissingIds_ReturnsMissingDataError()
        {
            var result = _friendRepository.SendFriendRequest(Guid.Empty, Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.MISSING_DATA));
        }

        [Test]
        public void SendFriendRequest_SelfRequest_ReturnsInvalidDataError()
        {
            Guid id = Guid.NewGuid();

            var result = _friendRepository.SendFriendRequest(id, id);

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
        }

        [Test]
        public void SendFriendRequest_DuplicateRequest_ReturnsDuplicateError()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = fromId, friendID = toId });

            var result = _friendRepository.SendFriendRequest(fromId, toId);

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
        }

        [Test]
        public void SendFriendRequest_DbUpdateException_ReturnsDbError()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            var result = _friendRepository.SendFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SendFriendRequest_EntityException_ReturnsDbError()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            var result = _friendRepository.SendFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void AcceptFriendRequest_RequestExists_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            var result = _friendRepository.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AcceptFriendRequest_RequestExists_UpdatesStatusToTrue()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            _friendRepository.AcceptFriendRequest(meId, requesterId);

            Assert.That(friendship.requestStatus, Is.True);
        }

        [Test]
        public void AcceptFriendRequest_RequestNotFound_ReturnsNotFoundError()
        {
            var result = _friendRepository.AcceptFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void AcceptFriendRequest_DbException_ReturnsDbError()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            var result = _friendRepository.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void RejectFriendRequest_RequestExists_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            var result = _friendRepository.RejectFriendRequest(meId, requesterId);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void RejectFriendRequest_RequestExists_RemovesFromContext()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var friendship = new Friendship { playerID = requesterId, friendID = meId, requestStatus = false };
            _friendshipsData.Add(friendship);

            _friendRepository.RejectFriendRequest(meId, requesterId);

            _friendshipSet.Verify(m => m.Remove(friendship), Times.Once);
        }

        [Test]
        public void RejectFriendRequest_RequestNotFound_ReturnsNotFoundError()
        {
            var result = _friendRepository.RejectFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void RejectFriendRequest_DbException_ReturnsDbError()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = requesterId, friendID = meId, requestStatus = false });
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            var result = _friendRepository.RejectFriendRequest(meId, requesterId);

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void RemoveFriend_FriendshipExists_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            var link1 = new Friendship { playerID = meId, friendID = friendId, requestStatus = true };
            var link2 = new Friendship { playerID = friendId, friendID = meId, requestStatus = true };
            _friendshipsData.Add(link1);
            _friendshipsData.Add(link2);

            var result = _friendRepository.RemoveFriend(meId, friendId);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void RemoveFriend_FriendshipExists_RemovesRangeFromContext()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            var link1 = new Friendship { playerID = meId, friendID = friendId, requestStatus = true };
            var link2 = new Friendship { playerID = friendId, friendID = meId, requestStatus = true };
            _friendshipsData.Add(link1);
            _friendshipsData.Add(link2);

            _friendRepository.RemoveFriend(meId, friendId);

            _friendshipSet.Verify(m => m.RemoveRange(It.Is<IEnumerable<Friendship>>(l => l.Count() == 2)), Times.Once);
        }

        [Test]
        public void RemoveFriend_FriendshipNotFound_ReturnsNotFoundError()
        {
            var result = _friendRepository.RemoveFriend(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.NOT_FOUND));
        }

        [Test]
        public void RemoveFriend_DbException_ReturnsDbError()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            _friendshipsData.Add(new Friendship { playerID = meId, friendID = friendId, requestStatus = true });
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            var result = _friendRepository.RemoveFriend(meId, friendId);

            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }
    }
}