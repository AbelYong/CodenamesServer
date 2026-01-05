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
        private Mock<IPlayerDAO> _playerDaoMock;
        private FriendService _friendService;
        private Queue<IFriendCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _friendDaoMock = new Mock<IFriendDAO>();
            _playerDaoMock = new Mock<IPlayerDAO>();
            _callbackQueue = new Queue<IFriendCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<IFriendCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : CreateMockCallback().Object);

            _friendService = new FriendService(
                _callbackProviderMock.Object,
                _friendDaoMock.Object,
                _playerDaoMock.Object
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
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback();
            _callbackQueue.Enqueue(callbackMock.Object);

            // Act & Assert
            Assert.DoesNotThrow(() => _friendService.Connect(playerId));
        }

        [Test]
        public void Connect_EmptyPlayerId_DoesNothing()
        {
            // Arrange
            Guid emptyId = Guid.Empty;

            // Act
            _friendService.Connect(emptyId);

            // Assert
            _callbackProviderMock.Verify(cp => cp.GetCallback<IFriendCallback>(), Times.Never);
        }

        [Test]
        public void Disconnect_ValidPlayerId_UnregistersCallback()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => _friendService.Disconnect(playerId));
        }

        [Test]
        public void SendFriendRequest_ValidRequest_ReturnsSuccessAndNotifiesTarget()
        {
            // Arrange
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();

            var fromPlayerEntity = CreateDataAccessPlayer(fromId, "SenderUser");
            _playerDaoMock.Setup(p => p.GetPlayerById(fromId)).Returns(fromPlayerEntity);

            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = true });

            var toPlayerCallback = CreateMockCallback();
            _callbackQueue.Enqueue(toPlayerCallback.Object);
            _friendService.Connect(toId);

            // Act
            var result = _friendService.SendFriendRequest(fromId, toId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.FRIEND_REQUEST_SENT.Equals(result.StatusCode));

            toPlayerCallback.Verify(c => c.NotifyNewFriendRequest(It.Is<Player>(p => p.PlayerID == fromId)), Times.Once);
        }

        [Test]
        public void SendFriendRequest_SameIds_ReturnsUnallowed()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            var result = _friendService.SendFriendRequest(id, id);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
            _friendDaoMock.Verify(d => d.SendFriendRequest(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void SendFriendRequest_DaoFails_ReturnsConflict()
        {
            // Arrange
            Guid fromId = Guid.NewGuid();
            Guid toId = Guid.NewGuid();

            _friendDaoMock.Setup(d => d.SendFriendRequest(fromId, toId))
                .Returns(new OperationResult { Success = false });

            // Act
            var result = _friendService.SendFriendRequest(fromId, toId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CONFLICT.Equals(result.StatusCode));
        }

        [Test]
        public void AcceptFriendRequest_ValidRequest_ReturnsFriendAddedAndNotifiesRequester()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();

            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerDaoMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayerEntity);

            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });

            var requesterCallback = CreateMockCallback();
            _callbackQueue.Enqueue(requesterCallback.Object);
            _friendService.Connect(requesterId);

            // Act
            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.FRIEND_ADDED.Equals(result.StatusCode));

            requesterCallback.Verify(c => c.NotifyFriendRequestAccepted(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void AcceptFriendRequest_DaoFails_ReturnsServerError()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();

            _friendDaoMock.Setup(d => d.AcceptFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = false });

            // Act
            var result = _friendService.AcceptFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void RejectFriendRequest_ValidRequest_ReturnsRejectedAndNotifiesRequester()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();

            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerDaoMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayerEntity);

            _friendDaoMock.Setup(d => d.RejectFriendRequest(meId, requesterId))
                .Returns(new OperationResult { Success = true });

            var requesterCallback = CreateMockCallback();
            _callbackQueue.Enqueue(requesterCallback.Object);
            _friendService.Connect(requesterId);

            // Act
            var result = _friendService.RejectFriendRequest(meId, requesterId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.FRIEND_REQUEST_REJECTED.Equals(result.StatusCode));

            requesterCallback.Verify(c => c.NotifyFriendRequestRejected(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RejectFriendRequest_DaoFails_ReturnsServerError()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.RejectFriendRequest(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(new OperationResult { Success = false });

            // Act
            var result = _friendService.RejectFriendRequest(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void RemoveFriend_ValidRequest_ReturnsRemovedAndNotifiesFriend()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            Guid friendId = Guid.NewGuid();

            var mePlayerEntity = CreateDataAccessPlayer(meId, "MeUser");
            _playerDaoMock.Setup(p => p.GetPlayerById(meId)).Returns(mePlayerEntity);

            _friendDaoMock.Setup(d => d.RemoveFriend(meId, friendId))
                .Returns(new OperationResult { Success = true });

            var friendCallback = CreateMockCallback();
            _callbackQueue.Enqueue(friendCallback.Object);
            _friendService.Connect(friendId);

            // Act
            var result = _friendService.RemoveFriend(meId, friendId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.FRIEND_REMOVED.Equals(result.StatusCode));

            friendCallback.Verify(c => c.NotifyFriendRemoved(It.Is<Player>(p => p.PlayerID == meId)), Times.Once);
        }

        [Test]
        public void RemoveFriend_DaoFails_ReturnsServerError()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.RemoveFriend(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(new OperationResult { Success = false });

            // Act
            var result = _friendService.RemoveFriend(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void SearchPlayers_ValidQuery_ReturnsFilteredListExcludingSelf()
        {
            // Arrange
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

            // Act
            var result = _friendService.SearchPlayers(query, meId, 0);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].PlayerID, Is.EqualTo(otherId));
        }

        [Test]
        public void SearchPlayers_NoResults_ReturnsEmptyList()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.SearchPlayers(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns((List<DataAccess.Player>)null);

            // Act
            var result = _friendService.SearchPlayers("query", Guid.NewGuid(), 10);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SearchPlayers_DaoException_ThrowsException()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.SearchPlayers(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Throws(new Exception("DB Error"));

            // Act & Assert
            Assert.Throws<Exception>(() => _friendService.SearchPlayers("test", Guid.NewGuid(), 10));
        }

        [Test]
        public void GetFriends_FriendsExist_ReturnsList()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            var friendsList = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(Guid.NewGuid(), "Friend1")
            };

            _friendDaoMock.Setup(d => d.GetFriends(meId)).Returns(friendsList);

            // Act
            var result = _friendService.GetFriends(meId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Username, Is.EqualTo("Friend1"));
        }

        [Test]
        public void GetFriends_NoFriends_ReturnsEmptyList()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.GetFriends(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            // Act
            var result = _friendService.GetFriends(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetIncomingRequests_RequestsExist_ReturnsList()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            var requests = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(Guid.NewGuid(), "Requester1")
            };

            _friendDaoMock.Setup(d => d.GetIncomingRequests(meId)).Returns(requests);

            // Act
            var result = _friendService.GetIncomingRequests(meId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Username, Is.EqualTo("Requester1"));
        }

        [Test]
        public void GetIncomingRequests_NoRequests_ReturnsEmptyList()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.GetIncomingRequests(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            // Act
            var result = _friendService.GetIncomingRequests(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetSentRequests_RequestsExist_ReturnsList()
        {
            // Arrange
            Guid meId = Guid.NewGuid();
            var sent = new List<DataAccess.Player>
            {
                CreateDataAccessPlayer(Guid.NewGuid(), "Target1")
            };

            _friendDaoMock.Setup(d => d.GetSentRequests(meId)).Returns(sent);

            // Act
            var result = _friendService.GetSentRequests(meId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetSentRequests_NoRequests_ReturnsEmptyList()
        {
            // Arrange
            _friendDaoMock.Setup(d => d.GetSentRequests(It.IsAny<Guid>()))
                .Returns((List<DataAccess.Player>)null);

            // Act
            var result = _friendService.GetSentRequests(Guid.NewGuid());

            // Assert
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