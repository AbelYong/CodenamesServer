using DataAccess.DataRequests;
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
using System.Linq;
using System.ServiceModel;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class FriendServiceTest
    {
        private Mock<ICallbackProvider> _callbackProviderMock;
        private Mock<IFriendRepository> _friendDaoMock;
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private FriendService _friendService;
        private Queue<IFriendCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _friendDaoMock = new Mock<IFriendRepository>();
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

        private static DataAccess.Player CreateDataAccessPlayer(Guid id, string username)
        {
            return new DataAccess.Player
            {
                playerID = id,
                username = username,
                User = new DataAccess.User { email = "test@test.com" }
            };
        }

        [Test]
        public void Connect_ValidPlayerId_RegistersCallback()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback();
            _callbackQueue.Enqueue(callbackMock.Object);

            _friendService.Connect(playerId);

            _callbackProviderMock.Verify(cp => cp.GetCallback<IFriendCallback>(), Times.Once);
        }

        [Test]
        public void Connect_EmptyPlayerId_DoesNothing()
        {
            Guid emptyId = Guid.Empty;

            _friendService.Connect(emptyId);

            _callbackProviderMock.Verify(cp => cp.GetCallback<IFriendCallback>(), Times.Never);
        }

        [Test]
        public void Disconnect_ValidPlayerId_DoesNotThrow()
        {
            Guid playerId = Guid.NewGuid();

            Assert.DoesNotThrow(() => _friendService.Disconnect(playerId));
        }

        [Test]
        public void SendFriendRequest_ValidRequest_ReturnsSuccess()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = true });
            var fromPlayer = CreateDataAccessPlayer(fromId, "Sender");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(fromId)).Returns(fromPlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(toId);

            var result = _friendService.SendFriendRequest(fromId, toId);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void SendFriendRequest_ValidRequest_NotifiesTarget()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = true });
            var fromPlayer = CreateDataAccessPlayer(fromId, "Sender");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(fromId)).Returns(fromPlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(toId);

            _friendService.SendFriendRequest(fromId, toId);

            callback.Verify(c => c.NotifyNewFriendRequest(It.Is<Player>(p => p.PlayerID == fromId)), Times.Once);
        }

        [Test]
        public void SendFriendRequest_SameIds_ReturnsUnallowed()
        {
            Guid id = Guid.NewGuid();

            var result = _friendService.SendFriendRequest(id, id);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.UNALLOWED));
        }

        [Test]
        public void SendFriendRequest_DaoFails_ReturnsServerError()
        {
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = false });

            var result = _friendService.SendFriendRequest(fromId, toId);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.SERVER_ERROR));
        }

        [Test]
        public void AcceptFriendRequest_ValidRequest_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(requesterId);

            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void AcceptFriendRequest_ValidRequest_NotifiesRequester()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(requesterId);

            _friendService.AcceptFriendRequest(meId, requesterId);

            callback.Verify(c => c.NotifyFriendRequestAccepted(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void AcceptFriendRequest_DaoFails_ReturnsServerError()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = false });

            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.SERVER_ERROR));
        }

        [Test]
        public void RejectFriendRequest_ValidRequest_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RejectFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(requesterId);

            var result = _friendService.RejectFriendRequest(meId, requesterId);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void RejectFriendRequest_ValidRequest_NotifiesRequester()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RejectFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(requesterId);

            _friendService.RejectFriendRequest(meId, requesterId);

            callback.Verify(c => c.NotifyFriendRequestRejected(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RejectFriendRequest_DaoFails_ReturnsServerError()
        {
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RejectFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = false });

            var result = _friendService.RejectFriendRequest(meId, requesterId);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.SERVER_ERROR));
        }

        [Test]
        public void RemoveFriend_ValidRequest_ReturnsSuccess()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RemoveFriend(meId, friendId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(friendId);

            var result = _friendService.RemoveFriend(meId, friendId);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void RemoveFriend_ValidRequest_NotifiesFriend()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RemoveFriend(meId, friendId))
                .Returns(new OperationResult { Success = true });
            var mePlayer = CreateDataAccessPlayer(meId, "Me");
            _playerRepositoryMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayer);
            var callback = CreateMockCallback();
            _callbackQueue.Enqueue(callback.Object);
            _friendService.Connect(friendId);

            _friendService.RemoveFriend(meId, friendId);

            callback.Verify(c => c.NotifyFriendRemoved(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RemoveFriend_DaoFails_ReturnsServerError()
        {
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();
            _friendDaoMock.Setup(d => d.RemoveFriend(meId, friendId))
                .Returns(new OperationResult { Success = false });

            var result = _friendService.RemoveFriend(meId, friendId);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.SERVER_ERROR));
        }

        [Test]
        public void SearchPlayers_ValidQuery_ReturnsFilteredList()
        {
            string query = "User";
            Guid meId = Guid.NewGuid();
            Guid otherId = Guid.NewGuid();
            var players = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(meId, "Me"),
                CreateDataAccessPlayer(otherId, "Other")
            };
            var response = new PlayerListRequest { IsSuccess = true, Players = players };
            _friendDaoMock.Setup(d => d.SearchPlayers(query, meId, 20)).Returns(response);

            var result = _friendService.SearchPlayers(query, meId, 0);

            Assert.That(result.FriendsList.Count, Is.EqualTo(1));
        }

        [Test]
        public void SearchPlayers_ValidQuery_ExcludesSelf()
        {
            string query = "User";
            Guid meId = Guid.NewGuid();
            Guid otherId = Guid.NewGuid();
            var players = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(meId, "Me"),
                CreateDataAccessPlayer(otherId, "Other")
            };
            var response = new PlayerListRequest { IsSuccess = true, Players = players };
            _friendDaoMock.Setup(d => d.SearchPlayers(query, meId, 20)).Returns(response);

            var result = _friendService.SearchPlayers(query, meId, 0);

            Assert.That(result.FriendsList.First().PlayerID, Is.EqualTo(otherId));
        }

        [Test]
        public void SearchPlayers_DaoFails_ReturnsDbError()
        {
            var response = new PlayerListRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR };
            _friendDaoMock.Setup(d => d.SearchPlayers(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns(response);

            var result = _friendService.SearchPlayers("query", Guid.NewGuid(), 10);

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.DATABASE_ERROR));
        }

        [Test]
        public void GetFriends_FriendsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            var friends = new List<DataAccess.Player> { CreateDataAccessPlayer(Guid.NewGuid(), "Friend") };
            var response = new PlayerListRequest { IsSuccess = true, Players = friends };
            _friendDaoMock.Setup(d => d.GetFriends(meId)).Returns(response);

            var result = _friendService.GetFriends(meId);

            Assert.That(result.FriendsList.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetFriends_DaoFails_ReturnsDbError()
        {
            var response = new PlayerListRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR };
            _friendDaoMock.Setup(d => d.GetFriends(It.IsAny<Guid>())).Returns(response);

            var result = _friendService.GetFriends(Guid.NewGuid());

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.DATABASE_ERROR));
        }

        [Test]
        public void GetIncomingRequests_RequestsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            var requests = new List<DataAccess.Player> { CreateDataAccessPlayer(Guid.NewGuid(), "Requester") };
            var response = new PlayerListRequest { IsSuccess = true, Players = requests };
            _friendDaoMock.Setup(d => d.GetIncomingRequests(meId)).Returns(response);

            var result = _friendService.GetIncomingRequests(meId);

            Assert.That(result.FriendsList.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetIncomingRequests_DaoFails_ReturnsDbError()
        {
            var response = new PlayerListRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR };
            _friendDaoMock.Setup(d => d.GetIncomingRequests(It.IsAny<Guid>())).Returns(response);

            var result = _friendService.GetIncomingRequests(Guid.NewGuid());

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.DATABASE_ERROR));
        }

        [Test]
        public void GetSentRequests_RequestsExist_ReturnsList()
        {
            Guid meId = Guid.NewGuid();
            var sent = new List<DataAccess.Player> { CreateDataAccessPlayer(Guid.NewGuid(), "Target") };
            var response = new PlayerListRequest { IsSuccess = true, Players = sent };
            _friendDaoMock.Setup(d => d.GetSentRequests(meId)).Returns(response);

            var result = _friendService.GetSentRequests(meId);

            Assert.That(result.FriendsList.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetSentRequests_DaoFails_ReturnsDbError()
        {
            var response = new PlayerListRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR };
            _friendDaoMock.Setup(d => d.GetSentRequests(It.IsAny<Guid>())).Returns(response);

            var result = _friendService.GetSentRequests(Guid.NewGuid());

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.DATABASE_ERROR));
        }
    }
}