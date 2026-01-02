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
    public class MatchmakingServiceTest
    {
        private Mock<ICallbackProvider> _callbackProviderMock;
        private MatchmakingService _matchmakingService;
        private Queue<IMatchmakingCallback> _callbackQueue;

        [SetUp]
        public void Setup()
        {
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _callbackQueue = new Queue<IMatchmakingCallback>();
            _callbackProviderMock.Setup(cp => cp.GetCallback<IMatchmakingCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<IMatchmakingCallback>().Object);

            _matchmakingService = new MatchmakingService(_callbackProviderMock.Object);
        }

        [Test]
        public void Connect_ValidPlayer_ReturnsSuccess()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callback = new Mock<IMatchmakingCallback>();
            _callbackQueue.Enqueue(callback.Object);

            // Act
            var result = _matchmakingService.Connect(playerId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void Connect_DuplicateConnection_UpdatesChannelAndReturnsSuccess()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // First connection
            _callbackQueue.Enqueue(new Mock<IMatchmakingCallback>().Object);
            _matchmakingService.Connect(playerId);

            // Second connection (reconnect)
            _callbackQueue.Enqueue(new Mock<IMatchmakingCallback>().Object);

            // Act
            var result = _matchmakingService.Connect(playerId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void RequestArrangedMatch_NullConfiguration_ReturnsMissingData()
        {
            // Act
            var result = _matchmakingService.RequestArrangedMatch(null);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void RequestArrangedMatch_BothPlayersConnected_SendsNotificationAndReturnsSuccess()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            var companionCallback = new Mock<IMatchmakingCallback>();

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            var config = new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId},
                Companion = new Player { PlayerID = companionId},
                MatchRules = new MatchRules { Gamemode = Gamemode.NORMAL }
            };

            // Act
            var result = _matchmakingService.RequestArrangedMatch(config);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.CREATED.Equals(result.StatusCode));
            requesterCallback.Verify(c => c.NotifyRequestPending(requesterId, companionId), Times.Once);
            companionCallback.Verify(c => c.NotifyRequestPending(requesterId, companionId), Times.Once);
        }

        [Test]
        public void RequestArrangedMatch_CompanionOffline_ReturnsClientUnreachable()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var offlineCompanionId = Guid.NewGuid();

            ConnectPlayer(requesterId, new Mock<IMatchmakingCallback>());
            // Companion is NOT connected

            var config = new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = offlineCompanionId },
                MatchRules = new MatchRules { Gamemode = Gamemode.NORMAL }
            };

            // Act
            var result = _matchmakingService.RequestArrangedMatch(config);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CLIENT_UNREACHABLE.Equals(result.StatusCode));
        }

        [Test]
        public void RequestArrangedMatch_CallbackThrowsException_ReturnsClientUnreachable()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            // Companion callback throws exception
            var companionCallback = new Mock<IMatchmakingCallback>();
            companionCallback.Setup(c => c.NotifyMatchReady(It.IsAny<Services.DTO.DataContract.Match>()))
                .Throws(new CommunicationException("Connection dropped"));

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            var config = new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                MatchRules = new MatchRules { Gamemode = Gamemode.NORMAL }
            };

            // Act
            var result = _matchmakingService.RequestArrangedMatch(config);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.CLIENT_UNREACHABLE.Equals(result.StatusCode));
        }

        [Test]
        public void ConfirmMatchReceived_BothPlayersConfirm_NotifiesPlayersReady()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            var companionCallback = new Mock<IMatchmakingCallback>();

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            Guid matchId = Guid.Empty;
            requesterCallback.Setup(c => c.NotifyMatchReady(It.IsAny<Services.DTO.DataContract.Match>()))
                .Callback<Services.DTO.DataContract.Match>(m => matchId = m.MatchID);

            var config = new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                MatchRules = new MatchRules { Gamemode = Gamemode.NORMAL }
            };
            _matchmakingService.RequestArrangedMatch(config);

            // Act
            _matchmakingService.ConfirmMatchReceived(requesterId, matchId);
            _matchmakingService.ConfirmMatchReceived(companionId, matchId);

            // Assert
            requesterCallback.Verify(c => c.NotifyPlayersReady(matchId), Times.Once);
            companionCallback.Verify(c => c.NotifyPlayersReady(matchId), Times.Once);
        }

        [Test]
        public void ConfirmMatchReceived_OnePlayerConfirms_DoesNotNotifyReadyYet()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            var companionCallback = new Mock<IMatchmakingCallback>();

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            Guid matchId = Guid.Empty;
            requesterCallback.Setup(c => c.NotifyMatchReady(It.IsAny<Services.DTO.DataContract.Match>()))
                .Callback<Services.DTO.DataContract.Match>(m => matchId = m.MatchID);

            _matchmakingService.RequestArrangedMatch(new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                MatchRules = new MatchRules()
            });

            // Act
            _matchmakingService.ConfirmMatchReceived(requesterId, matchId);

            // Assert
            requesterCallback.Verify(c => c.NotifyPlayersReady(It.IsAny<Guid>()), Times.Never);
            companionCallback.Verify(c => c.NotifyPlayersReady(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void RequestMatchCancel_MatchPending_RemovesMatchAndNotifiesCompanion()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            var companionCallback = new Mock<IMatchmakingCallback>();

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            Guid matchId = Guid.Empty;
            requesterCallback.Setup(c => c.NotifyMatchReady(It.IsAny<Services.DTO.DataContract.Match>()))
                .Callback<Services.DTO.DataContract.Match>(m => matchId = m.MatchID);

            _matchmakingService.RequestArrangedMatch(new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                MatchRules = new MatchRules()
            });

            // Act
            _matchmakingService.RequestMatchCancel(requesterId);

            // Assert
            companionCallback.Verify(c => c.NotifyMatchCanceled(matchId, StatusCode.CLIENT_CANCEL), Times.Once);
        }

        [Test]
        public void Disconnect_TheresPendingMatch_NotifiesCompanionAndCancelsMatch()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();

            var requesterCallback = new Mock<IMatchmakingCallback>();
            var companionCallback = new Mock<IMatchmakingCallback>();

            ConnectPlayer(requesterId, requesterCallback);
            ConnectPlayer(companionId, companionCallback);

            //Capture matchID
            Guid matchId = Guid.Empty;
            requesterCallback.Setup(c => c.NotifyMatchReady(It.IsAny<Services.DTO.DataContract.Match>()))
                .Callback<Services.DTO.DataContract.Match>(m => matchId = m.MatchID);

            _matchmakingService.RequestArrangedMatch(new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                MatchRules = new MatchRules()
            });

            // Act
            _matchmakingService.Disconnect(requesterId);

            // Assert - Verify companion was notified and player can reconnect
            companionCallback.Verify(c => c.NotifyMatchCanceled(matchId, StatusCode.CLIENT_CANCEL), Times.Once);
            var newCallback = new Mock<IMatchmakingCallback>();
            _callbackQueue.Enqueue(newCallback.Object);
            var result = _matchmakingService.Connect(requesterId);
            Assert.That(result.IsSuccess);
        }

        private void ConnectPlayer(Guid playerId, Mock<IMatchmakingCallback> callbackMock)
        {
            _callbackQueue.Enqueue(callbackMock.Object);
            _matchmakingService.Connect(playerId);
        }
    }
}