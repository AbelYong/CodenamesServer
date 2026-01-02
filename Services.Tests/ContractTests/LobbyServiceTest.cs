using DataAccess.Users;
using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.ServiceModel;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class LobbyServiceTest
    {
        private Mock<IPlayerDAO> _playerDaoMock;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private Mock<IEmailOperation> _emailOperationMock;
        private LobbyService _lobbyService;
        private System.Collections.Generic.Queue<ILobbyCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _playerDaoMock = new Mock<IPlayerDAO>();
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _emailOperationMock = new Mock<IEmailOperation>();
            _callbackQueue = new System.Collections.Generic.Queue<ILobbyCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<ILobbyCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<ILobbyCallback>().Object);

            _lobbyService = new LobbyService(_playerDaoMock.Object, _callbackProviderMock.Object, _emailOperationMock.Object);
        }

        [Test]
        public void Connect_NewPlayer_ReturnsSuccess()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callbackMock = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(callbackMock.Object);

            // Act
            var result = _lobbyService.Connect(playerId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void Connect_PlayerAlreadyConnected_ReconnectsAndReturnsSuccess()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // First connection
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(playerId);

            // Second connection (reconnect)
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);

            // Act
            var result = _lobbyService.Connect(playerId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void CreateParty_ValidPlayer_ReturnsCreatedAndLobbyCode()
        {
            // Arrange
            var player = CreateTestPlayer();

            // Act
            var result = _lobbyService.CreateParty(player);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.CREATED.Equals(result.StatusCode));
            Assert.That(result.LobbyCode, Is.Not.Null);
            Assert.That(6.Equals(result.LobbyCode.Length));
        }

        [Test]
        public void CreateParty_PlayerAlreadyInParty_ReturnsUnallowed()
        {
            // Arrange
            var player = CreateTestPlayer();
            _lobbyService.CreateParty(player); // First creation

            // Act
            var result = _lobbyService.CreateParty(player); // Try again

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
        }

        [Test]
        public void CreateParty_NullPlayer_ReturnsMissingData()
        {
            // Act
            var result = _lobbyService.CreateParty(null);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void InviteToParty_PlayerOnline_SendsCallbackInvitation()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guestId = Guid.NewGuid();
            string guestEmail = "guest@test.com";

            // 1. Host creates party
            var createResult = _lobbyService.CreateParty(host);
            string lobbyCode = createResult.LobbyCode;

            // 2. Connect Guest so they are "Online"
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guestId);

            // 3. Setup Email mocks
            _playerDaoMock.Setup(d => d.GetEmailByPlayerID(guestId)).Returns(guestEmail);
            _emailOperationMock.Setup(e => e.SendGameInvitationEmail(host.Username, guestEmail, lobbyCode)).Returns(true);

            // Act
            var result = _lobbyService.InviteToParty(host, guestId, lobbyCode);

            // Assert
            Assert.That(result.IsSuccess);
            guestCallback.Verify(c => c.NotifyMatchInvitationReceived(It.Is<Player>(p => p.PlayerID == host.PlayerID), lobbyCode), Times.Once);
        }

        [Test]
        public void InviteToParty_PlayerOffline_SendsEmailOnly()
        {
            // Arrange
            var host = CreateTestPlayer();
            var offlineGuestId = Guid.NewGuid();

            var createResult = _lobbyService.CreateParty(host);
            string lobbyCode = createResult.LobbyCode;

            _playerDaoMock.Setup(d => d.GetEmailByPlayerID(offlineGuestId)).Returns("offline@test.com");
            _emailOperationMock.Setup(e => e.SendGameInvitationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act (Guest is NOT connected)
            var result = _lobbyService.InviteToParty(host, offlineGuestId, lobbyCode);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
            _emailOperationMock.Verify(e => e.SendGameInvitationEmail(host.Username, "offline@test.com", lobbyCode), Times.Once);
        }

        [Test]
        public void InviteToParty_LobbyNotFound_ReturnsNotFound()
        {
            // Arrange
            var host = CreateTestPlayer();

            // Act
            var result = _lobbyService.InviteToParty(host, Guid.NewGuid(), "INVALID");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void InviteToParty_NotHost_ReturnsUnauthorized()
        {
            // Arrange
            var realHost = CreateTestPlayer();
            var impostor = CreateTestPlayer();

            var createResult = _lobbyService.CreateParty(realHost);

            // Act
            var result = _lobbyService.InviteToParty(impostor, Guid.NewGuid(), createResult.LobbyCode);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));
        }

        [Test]
        public void JoinParty_Success_NotifiesHost_ReturnsParty()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            // 1. Host Connects & Creates Party
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);

            var createResult = _lobbyService.CreateParty(host);
            string code = createResult.LobbyCode;

            // 2. Guest Connects
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);

            // Act
            var result = _lobbyService.JoinParty(guest, code);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
            Assert.That(result.Party, Is.Not.Null);
            Assert.That(guest.PlayerID.Equals(result.Party.PartyGuest.PlayerID));
            hostCallback.Verify(c => c.NotifyMatchInvitationAccepted(It.Is<Player>(p => p.PlayerID == guest.PlayerID)), Times.Once);
        }

        [Test]
        public void JoinParty_GuestNotConnected_ReturnsClientDisconnect()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            _lobbyService.CreateParty(host);
            // Guest does NOT call Connect()

            // Act
            var result = _lobbyService.JoinParty(guest, "SOMECODE");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CLIENT_DISCONNECT.Equals(result.StatusCode));
        }

        [Test]
        public void JoinParty_LobbyFull_ReturnsConflict()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest1 = CreateTestPlayer();
            var guest2 = CreateTestPlayer();

            // Host Setup
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);

            // Guest 1 Joins
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest1.PlayerID.Value);
            _lobbyService.JoinParty(guest1, createResult.LobbyCode);

            // Guest 2 Tries to Join
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest2.PlayerID.Value);

            // Act
            var result = _lobbyService.JoinParty(guest2, createResult.LobbyCode);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CONFLICT.Equals(result.StatusCode));
        }

        [Test]
        public void JoinParty_HostUnreachable_ReturnsClientUnreachable_AndRollsBack()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            // 1. Host Connects & Creates
            var hostCallback = new Mock<ILobbyCallback>();
            // Host notification throws exception
            hostCallback.Setup(c => c.NotifyMatchInvitationAccepted(It.IsAny<Player>()))
                .Throws(new CommunicationException("Host lost"));

            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);

            // 2. Guest Connects
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest.PlayerID.Value);

            // Act
            var result = _lobbyService.JoinParty(guest, createResult.LobbyCode);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CLIENT_UNREACHABLE.Equals(result.StatusCode));

            var createSecondParty = _lobbyService.CreateParty(guest);
            Assert.That(createSecondParty.IsSuccess);
            Assert.That(StatusCode.CREATED.Equals(createSecondParty.StatusCode));
        }

        [Test]
        public void LeaveParty_HostLeaves_RemovesLobby_NotifiesGuest()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            // Host Connect/Create
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);

            // Guest Connect/Join
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);

            // Act
            _lobbyService.LeaveParty(host.PlayerID.Value, partyRes.LobbyCode);

            // Assert
            // Guest should be notified that HOST (leavingPlayer) left
            guestCallback.Verify(c => c.NotifyPartyAbandoned(host.PlayerID.Value), Times.Once);

            // Verify Lobby is gone (try joining with a 3rd player)
            var p3 = CreateTestPlayer();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(p3.PlayerID.Value);
            var joinResult = _lobbyService.JoinParty(p3, partyRes.LobbyCode);
            Assert.That(StatusCode.NOT_FOUND.Equals(joinResult.StatusCode));
        }

        [Test]
        public void LeaveParty_GuestLeaves_LobbyRemains_NotifiesHost()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            // Host Connect/Create
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);

            // Guest Connect/Join
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);

            // Act
            _lobbyService.LeaveParty(guest.PlayerID.Value, partyRes.LobbyCode);

            // Assert
            // Host notified
            hostCallback.Verify(c => c.NotifyPartyAbandoned(guest.PlayerID.Value), Times.Once);

            // Verify Lobby exists and is open (Slot freed)
            var p3 = CreateTestPlayer();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);

            _lobbyService.Connect(p3.PlayerID.Value);
            var joinResult = _lobbyService.JoinParty(p3, partyRes.LobbyCode);

            Assert.That(joinResult.IsSuccess, "Spot was vacated, third payer can join");
            hostCallback.Verify(c => c.NotifyMatchInvitationAccepted(p3), Times.Once);
        }

        [Test]
        public void Disconnect_InLobby_TriggersAbandonLogic()
        {
            // Arrange
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();

            // Host Setup
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);

            // Guest Setup
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);

            // Act
            _lobbyService.Disconnect(host.PlayerID.Value);
            var joinSecondParty = _lobbyService.JoinParty(guest, partyRes.LobbyCode);

            // Assert - Verify Guest Notified and Party no longer exists
            Assert.That(!joinSecondParty.IsSuccess && joinSecondParty.StatusCode.Equals(StatusCode.NOT_FOUND));
            guestCallback.Verify(c => c.NotifyPartyAbandoned(It.Is<Guid>(p => p == host.PlayerID)), Times.Once);
        }

        private Player CreateTestPlayer()
        {
            return new Player
            {
                PlayerID = Guid.NewGuid(),
                Username = "TestUser_" + Guid.NewGuid().ToString().Substring(0, 5),
                User = new User { Email = "test@test.com" }
            };
        }
    }
}