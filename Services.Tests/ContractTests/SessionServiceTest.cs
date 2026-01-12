using DataAccess.DataRequests;
using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class SessionServiceTests
    {
        private Mock<IFriendManager> _friendManagerMock;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private SessionService _sessionService;
        private Queue<ISessionCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _friendManagerMock = new Mock<IFriendManager>();
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _callbackQueue = new Queue<ISessionCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<ISessionCallback>().Object);

            _sessionService = new SessionService(_friendManagerMock.Object, _callbackProviderMock.Object);
        }

        [Test]
        public void Connect_MissingPlayerData_ReturnsMissingDataError()
        {
            Player invalidPlayer = null;
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            var result = _sessionService.Connect(invalidPlayer);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Connect_ValidPlayer_ReturnsSuccess()
        {
            var player = CreateTestPlayer();
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>()))
                .Returns(new FriendListRequest());
            _callbackQueue.Enqueue(new Mock<ISessionCallback>().Object);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _sessionService.Connect(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Connect_PlayerHasOnlineFriends_NotifiesFriendsAndSendsList()
        {
            var incomingPlayer = CreateTestPlayer();
            var friendPlayer = CreateTestPlayer();
            FriendListRequest friendList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { friendPlayer }
            };
            FriendListRequest incomingList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { incomingPlayer }
            };
            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value))
                .Returns(friendList);
            _friendManagerMock.Setup(f => f.GetFriends(friendPlayer.PlayerID.Value))
                .Returns(incomingList);
            var incomingCallback = new Mock<ISessionCallback>();
            var friendCallback = new Mock<ISessionCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(friendCallback.Object);
            _sessionService.Connect(friendPlayer);
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(incomingCallback.Object);

            _sessionService.Connect(incomingPlayer);

            friendCallback.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p == incomingPlayer)), Times.Once);
            incomingCallback.Verify(c => c.ReceiveOnlineFriends(It.Is<List<Player>>(list => list.Count == 1 && list[0] == friendPlayer)), Times.Once);
        }

        [Test]
        public void Connect_FriendNotificationThrowsException_RemovesFriendFromOnline()
        {
            var incomingPlayer = CreateTestPlayer();
            var friendPlayer = CreateTestPlayer();
            FriendListRequest friendList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { friendPlayer }
            };
            FriendListRequest incomingList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { incomingPlayer }
            };
            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value))
                .Returns(friendList);
            _friendManagerMock.Setup(f => f.GetFriends(friendPlayer.PlayerID.Value))
                .Returns(incomingList);
            var friendCallbackMock = new Mock<ISessionCallback>();
            friendCallbackMock.Setup(c => c.NotifyFriendOnline(It.IsAny<Player>()))
                .Throws(new CommunicationException("Connection lost"));
            var incomingCallbackMock = new Mock<ISessionCallback>();
            _callbackQueue.Enqueue(friendCallbackMock.Object);
            _sessionService.Connect(friendPlayer);
            _callbackQueue.Enqueue(incomingCallbackMock.Object);

            _sessionService.Connect(incomingPlayer);

            Assert.That(_sessionService.IsPlayerOnline(friendPlayer.PlayerID.Value), Is.False);
        }

        [Test]
        public void Connect_SendOnlineFriendsThrowsException_DisconnectsNewPlayer()
        {
            var incomingPlayer = CreateTestPlayer();
            var friend = CreateTestPlayer();
            FriendListRequest friendList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { friend }
            };
            FriendListRequest incomingList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { incomingPlayer }
            };
            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value))
                .Returns(friendList);
            _friendManagerMock.Setup(f => f.GetFriends(friend.PlayerID.Value))
                .Returns(incomingList);
            var friendCallback = new Mock<ISessionCallback>();
            _callbackQueue.Enqueue(friendCallback.Object);
            _sessionService.Connect(friend);
            var incomingCallback = new Mock<ISessionCallback>();
            incomingCallback.Setup(c => c.ReceiveOnlineFriends(It.IsAny<List<Player>>()))
                .Throws(new TimeoutException("Client too slow"));
            _callbackQueue.Enqueue(incomingCallback.Object);

            _sessionService.Connect(incomingPlayer);

            Assert.That(_sessionService.IsPlayerOnline(incomingPlayer.PlayerID.Value), Is.False);
        }

        [Test]
        public void Disconnect_PlayerIsOnline_RemovesPlayerAndNotifiesFriends()
        {
            var playerToDisconnect = CreateTestPlayer();
            var friendOnline = CreateTestPlayer();
            var playerToDisconnectCallback = new Mock<ISessionCallback>();
            var friendCallback = new Mock<ISessionCallback>();
            ConnectPlayer(playerToDisconnect, playerToDisconnectCallback);
            ConnectPlayer(friendOnline, friendCallback);
            _callbackQueue.Enqueue(friendCallback.Object);
            FriendListRequest friendList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { friendOnline }
            };
            FriendListRequest leavingList = new FriendListRequest
            {
                IsSuccess = true,
                FriendsList = new List<Player> { playerToDisconnect }
            };
            _friendManagerMock.Setup(f => f.GetFriends(playerToDisconnect.PlayerID.Value))
                .Returns(friendList);
            _friendManagerMock.Setup(f => f.GetFriends(friendOnline.PlayerID.Value))
               .Returns(leavingList);

            _sessionService.Disconnect(playerToDisconnect);

            Assert.That(_sessionService.IsPlayerOnline(playerToDisconnect.PlayerID.Value), Is.False);
            friendCallback.Verify(c => c.NotifyFriendOffline(playerToDisconnect.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_BothPlayersOnline_NotifiesBoth()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>()))
                .Returns(new FriendListRequest());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackA.Object);
            _sessionService.Connect(playerA);
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackB.Object);
            _sessionService.Connect(playerB);

            _sessionService.NotifyNewFriendship(playerA, playerB);

            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_OnlyPlayerAOnline_OnlyNotifiesA()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>()))
                .Returns(new FriendListRequest());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackA.Object);
            _sessionService.Connect(playerA);
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackB.Object);

            _sessionService.NotifyNewFriendship(playerA, playerB);

            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Never);
        }

        [Test]
        public void NotifyNewFriendship_OnlyPlayerBOnline_OnlyNotifiesB()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackA.Object);
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>()))
                .Returns(new FriendListRequest());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(callbackB.Object);
            _sessionService.Connect(playerB);

            _sessionService.NotifyNewFriendship(playerB, playerA);

            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Never);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_CallbackThrowsException_RemovesFaultedPlayerDoesNotNotifyOnlinePlayer()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            callbackA.Setup(c => c.NotifyFriendOnline(It.IsAny<Player>()))
                .Throws(new CommunicationException());
            var callbackB = new Mock<ISessionCallback>();
            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            _sessionService.NotifyNewFriendship(playerA, playerB);

            Assert.That(_sessionService.IsPlayerOnline(playerA.PlayerID.Value), Is.False);
            callbackB.Verify(c => c.NotifyFriendOnline(playerA), Times.Never);
        }

        [Test]
        public void NotifyFriendshipEnded_Success_NotifiesBoth()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();
            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyFriendshipEnded_OnlyPlayerAOnline_OnlyNotifiesA()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();
            ConnectPlayer(playerA, callbackA);

            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Never);
        }

        [Test]
        public void NotifyFriendshipEnded_OnlyPlayerBOnline_OnlyNotifiesB()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();
            ConnectPlayer(playerB, callbackB);

            _sessionService.NotifyFriendshipEnded(playerB, playerA);

            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Never);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyFriendshipEnded_CallbackThrowsException_RemovesFaultedPlayerNotifiesOnlinePlayer()
        {
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();
            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();
            callbackB.Setup(c => c.NotifyFriendOffline(It.IsAny<Guid>()))
                .Throws(new TimeoutException());
            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            Assert.That(_sessionService.IsPlayerOnline(playerB.PlayerID.Value), Is.False);
            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once);
        }

        [Test]
        public void KickUser_PlayerOnline_KicksPlayerAndDisconnects()
        {
            var player = CreateTestPlayer();
            var callbackMock = new Mock<ISessionCallback>();
            ConnectPlayer(player, callbackMock);

            _sessionService.KickPlayer(player.PlayerID.Value, KickReason.PERMANTENT_BAN);

            callbackMock.Verify(c => c.NotifyKicked(KickReason.PERMANTENT_BAN), Times.Once);
            Assert.That(_sessionService.IsPlayerOnline(player.PlayerID.Value), Is.False);
        }

        [Test]
        public void KickUser_NotificationThrowsException_RemovesPlayer()
        {
            var player = CreateTestPlayer();
            var callbackMock = new Mock<ISessionCallback>();
            ConnectPlayer(player, callbackMock);
            callbackMock.Setup(c => c.NotifyKicked(It.IsAny<KickReason>()))
                .Throws(new CommunicationException("Simulated comm error"));

            _sessionService.KickPlayer(player.PlayerID.Value, KickReason.TEMPORARY_BAN);

            Assert.That(_sessionService.IsPlayerOnline(player.PlayerID.Value), Is.False);
        }

        private void ConnectPlayer(Player player, Mock<ISessionCallback> mockCallback)
        {
            _friendManagerMock.Setup(f => f.GetFriends(player.PlayerID.Value)).Returns(new FriendListRequest());
            _callbackQueue.Enqueue(mockCallback.Object);
            _sessionService.Connect(player);
        }

        private Player CreateTestPlayer()
        {
            return new Player
            {
                PlayerID = Guid.NewGuid(),
                Username = "User" + Guid.NewGuid().ToString().Substring(0, 4)
            };
        }
    }
}