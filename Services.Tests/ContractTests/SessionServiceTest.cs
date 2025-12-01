using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO;
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

            // Setup Provider to dequeue mocks so we can assign specific mocks to specific players
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<ISessionCallback>().Object);

            _sessionService = new SessionService(_friendManagerMock.Object, _callbackProviderMock.Object);
        }

        [Test]
        public void Connect_MissingPlayerData_ReturnsMissingDataError()
        {
            // Arrange
            Player invalidPlayer = null;

            // Act
            var result = _sessionService.Connect(invalidPlayer);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.StatusCode.Equals(StatusCode.MISSING_DATA));
        }

        [Test]
        public void Connect_ValidPlayer_ReturnsSuccess()
        {
            var player = CreateTestPlayer();
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());

            // Queue a fresh mock
            _callbackQueue.Enqueue(new Mock<ISessionCallback>().Object);

            var result = _sessionService.Connect(player);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_sessionService.IsPlayerOnline(player.PlayerID.Value), Is.True);
        }

        [Test]
        public void Connect_PlayerHasOnlineFriends_NotifiesFriendsAndReturnsList()
        {
            // Arrange
            var incomingPlayer = CreateTestPlayer();
            var friendPlayer = CreateTestPlayer(); // Already online

            // 1. Setup Friend Logic: They are friends with each other
            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value))
                .Returns(new List<Player> { friendPlayer });
            _friendManagerMock.Setup(f => f.GetFriends(friendPlayer.PlayerID.Value))
                .Returns(new List<Player> { incomingPlayer });

            // 2. Setup separate callbacks for the two players
            var incomingCallback = new Mock<ISessionCallback>();
            var friendCallback = new Mock<ISessionCallback>();

            // 3. Connect the "Friend" first to populate the internal dictionary
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(friendCallback.Object);
            _sessionService.Connect(friendPlayer);

            // 4. Now connect the "Incoming" player
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(incomingCallback.Object);

            // Act
            var result = _sessionService.Connect(incomingPlayer);

            // Assert
            // A. Verify the incoming player got success
            Assert.That(result.IsSuccess, Is.True);

            // B. Verify the Friend was notified that "IncomingPlayer" came online
            friendCallback.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == incomingPlayer.PlayerID)), Times.Once);

            // C. Verify the Incoming Player received the list containing "FriendPlayer"
            incomingCallback.Verify(c => c.ReceiveOnlineFriends(It.Is<List<Player>>(list => list.Count == 1 && list[0].PlayerID == friendPlayer.PlayerID)), Times.Once);
        }

        [Test]
        public void Connect_FriendNotificationThrowsException_RemovesFriendFromOnline()
        {
            // Arrange
            var incomingPlayer = CreateTestPlayer();
            var friendPlayer = CreateTestPlayer();

            // Setup friendship
            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value)).Returns(new List<Player> { friendPlayer });
            _friendManagerMock.Setup(f => f.GetFriends(friendPlayer.PlayerID.Value)).Returns(new List<Player> { incomingPlayer });

            // 1. Prepare Friend Callback (Faulty)
            var friendCallbackMock = new Mock<ISessionCallback>();
            // Simulate connection loss when notified about the new player
            friendCallbackMock.Setup(c => c.NotifyFriendOnline(It.IsAny<Player>()))
                .Throws(new CommunicationException("Connection lost"));

            // 2. Prepare Incoming Player Callback (Healthy)
            var incomingCallbackMock = new Mock<ISessionCallback>();

            // 3. Connect Friend First
            _callbackQueue.Enqueue(friendCallbackMock.Object);
            _sessionService.Connect(friendPlayer);

            // 4. Connect Incoming Player
            _callbackQueue.Enqueue(incomingCallbackMock.Object);

            // Act
            _sessionService.Connect(incomingPlayer);

            // Assert
            // Friend should be removed because their callback threw an exception
            Assert.That(_sessionService.IsPlayerOnline(friendPlayer.PlayerID.Value), Is.False);
            // Incoming player should still be online
            Assert.That(_sessionService.IsPlayerOnline(incomingPlayer.PlayerID.Value), Is.True);
        }

        [Test]
        public void Connect_SendOnlineFriendsThrowsException_DisconnectsNewPlayer()
        {
            // Arrange
            var incomingPlayer = CreateTestPlayer();
            var friend = CreateTestPlayer();

            _friendManagerMock.Setup(f => f.GetFriends(incomingPlayer.PlayerID.Value)).Returns(new List<Player> { friend });
            _friendManagerMock.Setup(f => f.GetFriends(friend.PlayerID.Value)).Returns(new List<Player> { incomingPlayer });

            // Friend connects first
            var friendCallback = new Mock<ISessionCallback>();
            _callbackQueue.Enqueue(friendCallback.Object);
            _sessionService.Connect(friend);

            // Incoming player connects, but fails to receive the friend list
            var incomingCallback = new Mock<ISessionCallback>();
            incomingCallback.Setup(c => c.ReceiveOnlineFriends(It.IsAny<List<Player>>()))
                .Throws(new TimeoutException("Client too slow"));

            _callbackQueue.Enqueue(incomingCallback.Object);

            // Act
            _sessionService.Connect(incomingPlayer);

            // Assert
            Assert.That(_sessionService.IsPlayerOnline(incomingPlayer.PlayerID.Value), Is.False); //Channel faulted between connection and friend reception
        }

        [Test]
        public void Disconnect_PlayerIsOnline_RemovesPlayerAndNotifiesFriends()
        {
            // Arrange
            var playerToDisconnect = CreateTestPlayer();
            var friendOnline = CreateTestPlayer();

            var playerToDisconnectCallback = new Mock<ISessionCallback>();
            var friendCallback = new Mock<ISessionCallback>();

            // Connect both players first
            ConnectPlayer(playerToDisconnect, playerToDisconnectCallback);
            ConnectPlayer(friendOnline, friendCallback);
            _callbackQueue.Enqueue(friendCallback.Object);

            // Setup friendship
            _friendManagerMock.Setup(f => f.GetFriends(playerToDisconnect.PlayerID.Value))
                .Returns(new List<Player> { friendOnline });
            _friendManagerMock.Setup(f => f.GetFriends(friendOnline.PlayerID.Value))
               .Returns(new List<Player> { playerToDisconnect });

            // Act
            _sessionService.Disconnect(playerToDisconnect);

            // Assert
            // 1. Verify player is offline
            Assert.That(_sessionService.IsPlayerOnline(playerToDisconnect.PlayerID.Value), Is.False);

            // 2. Verify friend was notified
            friendCallback.Verify(c => c.NotifyFriendOffline(playerToDisconnect.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_BothPlayersOnline_NotifiesBoth()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            // Connect A
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackA.Object);
            _sessionService.Connect(playerA);

            // Connect B
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackB.Object);
            _sessionService.Connect(playerB);

            // Act
            _sessionService.NotifyNewFriendship(playerA, playerB);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_OnlyPlayerAOnline_OnlyNotifiesA()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            // Connect A
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackA.Object);
            _sessionService.Connect(playerA);

            //B is not connected
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackB.Object);

            // Act
            _sessionService.NotifyNewFriendship(playerA, playerB);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Never);
        }

        [Test]
        public void NotifyNewFriendship_OnlyPlayerBOnline_OnlyNotifiesB()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            // A is not connected
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackA.Object);

            // Connect B 
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());
            _callbackProviderMock.Setup(cp => cp.GetCallback<ISessionCallback>()).Returns(callbackB.Object);
            _sessionService.Connect(playerB);

            // Act
            _sessionService.NotifyNewFriendship(playerB, playerA);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerB.PlayerID)), Times.Never);
            callbackB.Verify(c => c.NotifyFriendOnline(It.Is<Player>(p => p.PlayerID == playerA.PlayerID)), Times.Once);
        }

        [Test]
        public void NotifyNewFriendship_CallbackThrowsException_RemovesFaultedPlayer()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            // P1 fails to receive notification
            callbackA.Setup(c => c.NotifyFriendOnline(It.IsAny<Player>())).Throws(new CommunicationException());

            var callbackB = new Mock<ISessionCallback>(); // P2 is fine

            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            // Act
            _sessionService.NotifyNewFriendship(playerA, playerB);

            // Assert
            Assert.That(_sessionService.IsPlayerOnline(playerA.PlayerID.Value), Is.False);
            Assert.That(_sessionService.IsPlayerOnline(playerB.PlayerID.Value), Is.True);

            // Verify P2 was still notified (logic shouldn't stop if one fails)
            callbackB.Verify(c => c.NotifyFriendOnline(playerA), Times.Once);
        }

        [Test]
        public void NotifyFriendshipEnded_Success_NotifiesBoth()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            // Act
            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyFriendshipEnded_OnlyPlayerAOnline_OnlyNotifiesA()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            ConnectPlayer(playerA, callbackA);

            // Act
            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Never);
        }

        [Test]
        public void NotifyFriendshipEnded_OnlyPlayerBOnline_OnlyNotifiesB()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>();
            var callbackB = new Mock<ISessionCallback>();

            ConnectPlayer(playerB, callbackB);

            // Act
            _sessionService.NotifyFriendshipEnded(playerB, playerA);

            // Assert
            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Never);
            callbackB.Verify(c => c.NotifyFriendOffline(playerA.PlayerID.Value), Times.Once);
        }

        [Test]
        public void NotifyFriendshipEnded_CallbackThrowsException_RemovesFaultedPlayer()
        {
            // Arrange
            var playerA = CreateTestPlayer();
            var playerB = CreateTestPlayer();

            var callbackA = new Mock<ISessionCallback>(); // Healthy
            var callbackB = new Mock<ISessionCallback>(); // Faulty
            callbackB.Setup(c => c.NotifyFriendOffline(It.IsAny<Guid>())).Throws(new TimeoutException());

            ConnectPlayer(playerA, callbackA);
            ConnectPlayer(playerB, callbackB);

            // Act
            _sessionService.NotifyFriendshipEnded(playerA, playerB);

            // Assert
            Assert.That(_sessionService.IsPlayerOnline(playerA.PlayerID.Value), Is.True);
            Assert.That(_sessionService.IsPlayerOnline(playerB.PlayerID.Value), Is.False);
            callbackA.Verify(c => c.NotifyFriendOffline(playerB.PlayerID.Value), Times.Once, "P1 should still be notified.");
        }

        [Test]
        public void KickUser_PlayerOnline_NotifiesAndDisconnects()
        {
            // Arrange
            var player = CreateTestPlayer();
            var callback = new Mock<ISessionCallback>();
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());

            // Connect
            ConnectPlayer(player, callback);

            // Act
            _sessionService.KickUser(player.PlayerID.Value, BanReason.PermanentBan);

            // Assert
            // 1. Verify notification sent
            callback.Verify(c => c.NotifyKicked(BanReason.PermanentBan), Times.Once);

            // 2. Verify player disconnected
            Assert.That(_sessionService.IsPlayerOnline(player.PlayerID.Value), Is.False);
        }

        [Test]
        public void KickUser_NotificationThrowsException_RemovesPlayer()
        {
            // Arrange
            var player = CreateTestPlayer();
            _friendManagerMock.Setup(f => f.GetFriends(It.IsAny<Guid>())).Returns(new List<Player>());

            var callbackMock = new Mock<ISessionCallback>();
            callbackMock.Setup(c => c.NotifyKicked(It.IsAny<BanReason>()))
                .Throws(new CommunicationException("Simulated comm error"));

            _callbackQueue.Enqueue(callbackMock.Object);
            _sessionService.Connect(player);

            // Act
            _sessionService.KickUser(player.PlayerID.Value, BanReason.TemporaryBan);

            // Assert
            Assert.That(_sessionService.IsPlayerOnline(player.PlayerID.Value), Is.False);
        }

        private void ConnectPlayer(Player player, Mock<ISessionCallback> mockCallback)
        {
            _friendManagerMock.Setup(f => f.GetFriends(player.PlayerID.Value)).Returns(new List<Player>());
            _callbackQueue.Enqueue(mockCallback.Object);
            _sessionService.Connect(player);
        }

        private Player CreateTestPlayer()
        {
            return new Player
            {
                PlayerID = Guid.NewGuid(),
                Username = "User_" + Guid.NewGuid().ToString().Substring(0, 4)
            };
        }
    }
}