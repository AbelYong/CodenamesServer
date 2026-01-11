using DataAccess.Users;
using DataAccess.Util;
using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class FriendServiceTest
    {
        private Mock<ICallbackProvider> _callbackProviderMock;
        private Mock<IFriendDAO> _friendDaoMock;
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private FriendService _friendService;
        private Queue<IFriendCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _friendDaoMock = new Mock<IFriendDAO>();
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _callbackQueue = new Queue<IFriendCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<IFriendCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : CreateMockCallback().Object);

            _friendService = new FriendService(
                _callbackProviderMock.Object,
                _friendDaoMock.Object,
                _playerRepositoryMock.Object
            );
        }

        private static Mock<IFriendCallback> CreateMockCallback()
        {
            var mock = new Mock<IFriendCallback>();
            mock.As<ICommunicationObject>();
            return mock;
        }

        [Test]
        public void Connect_ValidPlayerId_RegistersCallback()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback();
            _callbackQueue.Enqueue(callbackMock.Object);

            Assert.DoesNotThrow(() => _friendService.Connect(playerId));
        }

        [Test]
        public void Connect_EmptyPlayerId_DoesNothing()
        {
            Guid emptyId = Guid.Empty;

            _friendService.Connect(emptyId);

            _callbackProviderMock.Verify(cp => cp.GetCallback<IFriendCallback>(), Times.Never);
        }

        [Test]
        public void Disconnect_ValidPlayerId_UnregistersCallback()
        {
            Guid playerId = Guid.NewGuid();

            Assert.DoesNotThrow(() => _friendService.Disconnect(playerId));
        }

        [Test]
        public void SendFriendRequest_ValidRequest_ReturnsSuccessAndNotifiesTarget()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            var fromPlayerEntity = CreateDataAccessPlayer(fromId, "SenderUser");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(fromId))
                .Returns(fromPlayerEntity);
            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = true });
            var toPlayerCallback = CreateMockCallback();
            _callbackQueue.Enqueue(toPlayerCallback.Object);
            _friendService.Connect(toId);
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.FRIEND_REQUEST_SENT
            };

            var result = _friendService.SendFriendRequest(fromId, toId);

            Assert.That(result.Equals(expected));
            toPlayerCallback.Verify(c => c.NotifyNewFriendRequest(It.Is<Player>(p => p.PlayerID == fromId)), Times.Once);
        }

        [Test]
        public void SendFriendRequest_SameIds_ReturnsUnallowedDoesNotNotifyTarget()
        {
            Guid id = Guid.NewGuid();
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED
            };

            var result = _friendService.SendFriendRequest(id, id);

            Assert.That(result.Equals(expected));
            _friendDaoMock.Verify(d => d.SendFriendRequest(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void SendFriendRequest_DaoFails_ReturnsServerError()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = false });
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _friendService.SendFriendRequest(fromId, toId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void AcceptFriendRequest_ValidRequest_ReturnsFriendAddedAndNotifiesRequester()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId))
                .Returns(mePlayerEntity);
            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var requesterCallback = CreateMockCallback();
            _callbackQueue.Enqueue(requesterCallback.Object);
            _friendService.Connect(requesterId);
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.FRIEND_ADDED
            };

            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.Equals(expected));
            requesterCallback.Verify(c => c.NotifyFriendRequestAccepted(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void AcceptFriendRequest_DaoFails_ReturnsServerError()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = false });
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void RejectFriendRequest_ValidRequest_ReturnsRejectedAndNotifiesRequester()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayerEntity);
            _friendDaoMock.Setup(d => d.RejectFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var requesterCallback = CreateMockCallback();
            _callbackQueue.Enqueue(requesterCallback.Object);
            _friendService.Connect(requesterId);
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.FRIEND_REQUEST_REJECTED
            };

            var result = _friendService.RejectFriendRequest(meId, requesterId);

            Assert.That(result.Equals(expected));
            requesterCallback.Verify(c => c.NotifyFriendRequestRejected(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RejectFriendRequest_DaoFails_ReturnsServerError()
        {
            _friendDaoMock.Setup(d => d.RejectFriendRequest(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(new OperationResult { Success = false });
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _friendService.RejectFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void RemoveFriend_ValidRequest_ReturnsRemovedAndNotifiesFriend()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayerEntity);
            _friendDaoMock.Setup(d => d.RemoveFriend(meId, friendId))
                .Returns(new OperationResult { Success = true });
            var friendCallback = CreateMockCallback();
            _callbackQueue.Enqueue(friendCallback.Object);
            _friendService.Connect(friendId);
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.FRIEND_REMOVED
            };

            var result = _friendService.RemoveFriend(meId, friendId);

            Assert.That(result.Equals(expected));
            friendCallback.Verify(c => c.NotifyFriendRemoved(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RemoveFriend_DaoFails_ReturnsServerError()
        {
            _friendDaoMock.Setup(d => d.RemoveFriend(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(new OperationResult { Success = false });
            FriendshipRequest expected = new FriendshipRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _friendService.RemoveFriend(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SearchPlayers_ValidQuery_ReturnsFilteredListExcludingSelf()
        {
            string query = "User";
            Guid meId = Guid.NewGuid();
            Guid otherId = Guid.NewGuid();
            var playerList = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(meId, "UserMe"),
                CreateDataAccessPlayer(otherId, "UserOther")
            };
            _friendDaoMock.Setup(d => d.SearchPlayers(query, meId, 20))
                .Returns(playerList);
            List<Player> expected = new List<Player>();
            expected.Add(item: new Player { PlayerID = otherId });

            var result = _friendService.SearchPlayers(query, meId, 0);

            Assert.That(result[0].Equals(expected[0]));
        }

        [Test]
        public void SearchPlayers_NoResults_ReturnsEmptyList()
        {
            _friendDaoMock.Setup(d => d.SearchPlayers(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns((List<DataAccess.Player>)null);

            var result = _friendService.SearchPlayers("query", Guid.NewGuid(), 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_DaoException_ThrowsException()
        {
            _friendDaoMock.Setup(d => d.SearchPlayers(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Throws(new Exception("DB Error"));

            Assert.Throws<Exception>(() => _friendService.SearchPlayers("test", Guid.NewGuid(), 10));
        }

        [Test]
        public void GetFriends_FriendsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            Guid friendID = Guid.NewGuid();
            var friendsList = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(friendID, "Friend1")
            };
            _friendDaoMock.Setup(d => d.GetFriends(meId)).Returns(friendsList);
            List<Player> expected = new List<Player>();
            expected.Add(item: new Player { PlayerID = friendID, Username = "Friend1" });

            var result = _friendService.GetFriends(meId);

            Assert.That(result[0].Equals(expected[0]));
        }

        [Test]
        public void GetFriends_NoFriends_ReturnsEmptyList()
        {
            _friendDaoMock.Setup(d => d.GetFriends(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            var result = _friendService.GetFriends(Guid.NewGuid());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetIncomingRequests_RequestsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            var requests = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(friendId, "Requester1")
            };
            _friendDaoMock.Setup(d => d.GetIncomingRequests(meId)).Returns(requests);
            List<Player> expected = new List<Player>();
            expected.Add(item: new Player { PlayerID = friendId, Username = "Requester1" });

            var result = _friendService.GetIncomingRequests(meId);

            Assert.That(result[0].Equals(expected[0]));
        }

        [Test]
        public void GetIncomingRequests_NoRequests_ReturnsEmptyList()
        {
            _friendDaoMock.Setup(d => d.GetIncomingRequests(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            var result = _friendService.GetIncomingRequests(Guid.NewGuid());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetSentRequests_RequestsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();
            var sent = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(targetId, "Target1")
            };
            _friendDaoMock.Setup(d => d.GetSentRequests(meId)).Returns(sent);
            List<Player> expected = new List<Player>();
            expected.Add(item: new Player { PlayerID = targetId, Username = "Requester1" });

            var result = _friendService.GetSentRequests(meId);

            Assert.That(result[0].Equals(expected[0]));
        }

        [Test]
        public void GetSentRequests_NoRequests_ReturnsEmptyList()
        {
            _friendDaoMock.Setup(d => d.GetSentRequests(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            var result = _friendService.GetSentRequests(Guid.NewGuid());

            Assert.That(result, Is.Empty);
        }

        private static DataAccess.Player CreateDataAccessPlayer(Guid id, string username)
        {
            return new DataAccess.Player
            {
                playerID = id,
                username = username,
                User = new DataAccess.User { email = "test@test.com" }
            };
        }
    }
}