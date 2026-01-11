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
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private Mock<IEmailOperation> _emailOperationMock;
        private LobbyService _lobbyService;
        private System.Collections.Generic.Queue<ILobbyCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _emailOperationMock = new Mock<IEmailOperation>();
            _callbackQueue = new System.Collections.Generic.Queue<ILobbyCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<ILobbyCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<ILobbyCallback>().Object);

            _lobbyService = new LobbyService(_callbackProviderMock.Object, _playerRepositoryMock.Object, _emailOperationMock.Object);
        }

        [Test]
        public void Connect_NewPlayer_ReturnsSuccess()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(callbackMock.Object);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _lobbyService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Connect_PlayerAlreadyConnected_ReconnectsAndReturnsSuccess()
        {
            Guid playerId = Guid.NewGuid();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(playerId);
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            var result = _lobbyService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void CreateParty_ValidPlayer_ReturnsCreatedAndLobbyCode()
        {
            var player = CreateTestPlayer();
            _lobbyService.Connect(player.PlayerID.Value);

            var result = _lobbyService.CreateParty(player);

            Assert.That(result.StatusCode.Equals(StatusCode.CREATED) && result.LobbyCode.Length.Equals(6));
        }

        [Test]
        public void CreateParty_PlayerAlreadyInParty_ReturnsUnallowed()
        {
            var player = CreateTestPlayer();
            InitializeParty(player);
            CreateLobbyRequest expected = new CreateLobbyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED,
                LobbyCode = string.Empty
            };

            var result = _lobbyService.CreateParty(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void CreateParty_NullPlayer_ReturnsMissingData()
        {
            CreateLobbyRequest expected = new CreateLobbyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA,
                LobbyCode = string.Empty
            };

            var result = _lobbyService.CreateParty(null);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void InviteToParty_PlayerOnline_ReturnsSuccessSendsCallbackInvitation()
        {
            var host = CreateTestPlayer();
            var guestId = Guid.NewGuid();
            string guestEmail = "guest@test.com";
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);
            string lobbyCode = createResult.LobbyCode;
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guestId);
            _playerRepositoryMock.Setup(d => d.GetEmailByPlayerID(guestId))
                .Returns(guestEmail);
            _emailOperationMock.Setup(e => e.SendGameInvitationEmail(host.Username, guestEmail, lobbyCode))
                .Returns(true);

            var result = _lobbyService.InviteToParty(host, guestId, lobbyCode);

            Assert.That(result.IsSuccess);
            guestCallback.Verify(c => c.NotifyMatchInvitationReceived(It.Is<Player>(p => p.PlayerID == host.PlayerID), lobbyCode), Times.Once);
        }

        [Test]
        public void InviteToParty_PlayerOffline_ReturnsSuccessOnlySendsEmail()
        {
            var host = CreateTestPlayer();
            var offlineGuestId = Guid.NewGuid();
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);
            string lobbyCode = createResult.LobbyCode;
            _playerRepositoryMock.Setup(d => d.GetEmailByPlayerID(offlineGuestId)).Returns("offline@test.com");
            _emailOperationMock.Setup(e => e.SendGameInvitationEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var result = _lobbyService.InviteToParty(host, offlineGuestId, lobbyCode);

            Assert.That(result.IsSuccess);
            _emailOperationMock.Verify(e => e.SendGameInvitationEmail(host.Username, "offline@test.com", lobbyCode), Times.Once);
        }

        [Test]
        public void InviteToParty_LobbyNotFound_ReturnsNotFound()
        {
            var host = CreateTestPlayer();
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _lobbyService.InviteToParty(host, Guid.NewGuid(), "INVALID");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void InviteToParty_RequesterIsNotHost_ReturnsUnauthorized()
        {
            var realHost = CreateTestPlayer();
            var impostor = CreateTestPlayer();
            _lobbyService.Connect(realHost.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(realHost);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED
            };

            var result = _lobbyService.InviteToParty(impostor, Guid.NewGuid(), createResult.LobbyCode);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinParty_Success_ReturnsPartyNotifiesHost()
        {
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);
            string code = createResult.LobbyCode;
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            JoinPartyRequest expected = new JoinPartyRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK,
                Party = new Party
                {
                    PartyHost = host,
                    PartyGuest = guest,
                    LobbyCode = code
                }
            };

            var result = _lobbyService.JoinParty(guest, code);

            Assert.That(result.Equals(expected));
            hostCallback.Verify(c => c.NotifyMatchInvitationAccepted(It.Is<Player>(p => p.PlayerID == guest.PlayerID)), Times.Once);
        }

        [Test]
        public void JoinParty_GuestNotConnected_ReturnsClientDisconnect()
        {
            var host = CreateTestPlayer();
            _lobbyService.Connect(host.PlayerID.Value);
            var guest = CreateTestPlayer();
            JoinPartyRequest expected = new JoinPartyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.CLIENT_DISCONNECT
            };

            _lobbyService.CreateParty(host);

            var result = _lobbyService.JoinParty(guest, "SOMECODE");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinParty_LobbyFull_ReturnsConflict()
        {
            var host = CreateTestPlayer();
            var guest1 = CreateTestPlayer();
            var guest2 = CreateTestPlayer();
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest1.PlayerID.Value);
            _lobbyService.JoinParty(guest1, createResult.LobbyCode);
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest2.PlayerID.Value);
            JoinPartyRequest expected = new JoinPartyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.CONFLICT
            };

            var result = _lobbyService.JoinParty(guest2, createResult.LobbyCode);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinParty_HostUnreachable_ReturnsClientUnreachable_AndRollsbackJoin()
        {
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();
            var hostCallback = new Mock<ILobbyCallback>();
            hostCallback.Setup(c => c.NotifyMatchInvitationAccepted(It.IsAny<Player>()))
                .Throws(new CommunicationException("Host lost"));
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var createResult = _lobbyService.CreateParty(host);
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            JoinPartyRequest expectedFailure = new JoinPartyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.CLIENT_UNREACHABLE,
            };

            var failureResult = _lobbyService.JoinParty(guest, createResult.LobbyCode);
            var rollbackResultVerification = _lobbyService.CreateParty(guest);

            Assert.That(failureResult.Equals(expectedFailure) && rollbackResultVerification.StatusCode.Equals(StatusCode.CREATED));
        }

        [Test]
        public void LeaveParty_HostLeaves_NotifiesGuestRemovesLobby()
        {
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);
            var player3 = CreateTestPlayer();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(player3.PlayerID.Value);

            _lobbyService.LeaveParty(host.PlayerID.Value, partyRes.LobbyCode);
            var joinResult = _lobbyService.JoinParty(player3, partyRes.LobbyCode);

            guestCallback.Verify(c => c.NotifyPartyAbandoned(host.PlayerID.Value), Times.Once);
            Assert.That(StatusCode.NOT_FOUND.Equals(joinResult.StatusCode));
        }

        [Test]
        public void LeaveParty_GuestLeaves_LobbyRemains_NotifiesHost()
        {
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);
            var player3 = CreateTestPlayer();
            _callbackQueue.Enqueue(new Mock<ILobbyCallback>().Object);
            _lobbyService.Connect(player3.PlayerID.Value);

            _lobbyService.LeaveParty(guest.PlayerID.Value, partyRes.LobbyCode);
            var joinResult = _lobbyService.JoinParty(player3, partyRes.LobbyCode);

            hostCallback.Verify(c => c.NotifyPartyAbandoned(guest.PlayerID.Value), Times.Once);
            Assert.That(joinResult.IsSuccess);
        }

        [Test]
        public void Disconnect_InLobby_TriggersAbandonLogic()
        {
            var host = CreateTestPlayer();
            var guest = CreateTestPlayer();
            var hostCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(hostCallback.Object);
            _lobbyService.Connect(host.PlayerID.Value);
            var partyRes = _lobbyService.CreateParty(host);
            var guestCallback = new Mock<ILobbyCallback>();
            _callbackQueue.Enqueue(guestCallback.Object);
            _lobbyService.Connect(guest.PlayerID.Value);
            _lobbyService.JoinParty(guest, partyRes.LobbyCode);
            JoinPartyRequest expectedFailure = new JoinPartyRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            _lobbyService.Disconnect(host.PlayerID.Value);
            var joinRemovedParty = _lobbyService.JoinParty(guest, partyRes.LobbyCode);

            Assert.That(joinRemovedParty.Equals(expectedFailure));
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

        private void InitializeParty(Player partyHost)
        {
            Guid auxID = (Guid)partyHost.PlayerID;
            _lobbyService.Connect(auxID);
            _lobbyService.CreateParty(partyHost);
        }
    }
}