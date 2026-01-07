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
            Guid playerId = Guid.NewGuid();
            var callback = new Mock<IMatchmakingCallback>();
            _callbackQueue.Enqueue(callback.Object);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK,
            };

            var result = _matchmakingService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Connect_DuplicateConnection_UpdatesChannelAndReturnsSuccess()
        {
            Guid playerId = Guid.NewGuid();
            _callbackQueue.Enqueue(new Mock<IMatchmakingCallback>().Object);
            _matchmakingService.Connect(playerId);
            _callbackQueue.Enqueue(new Mock<IMatchmakingCallback>().Object);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK,
            };

            var result = _matchmakingService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void RequestArrangedMatch_NullConfiguration_ReturnsMissingData()
        {
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA,
            };

            var result = _matchmakingService.RequestArrangedMatch(null);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void RequestArrangedMatch_BothPlayersConnected_SendsNotificationAndReturnsSuccess()
        {
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
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.CREATED,
            };

            var result = _matchmakingService.RequestArrangedMatch(config);

            Assert.That(result.Equals(expected));
            requesterCallback.Verify(c => c.NotifyRequestPending(requesterId, companionId), Times.Once);
            companionCallback.Verify(c => c.NotifyRequestPending(requesterId, companionId), Times.Once);
        }

        [Test]
        public void RequestArrangedMatch_CompanionOffline_ReturnsClientUnreachable()
        {
            var requesterId = Guid.NewGuid();
            var offlineCompanionId = Guid.NewGuid();
            ConnectPlayer(requesterId, new Mock<IMatchmakingCallback>());
            var config = new MatchConfiguration
            {
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = offlineCompanionId },
                MatchRules = new MatchRules { Gamemode = Gamemode.NORMAL }
            };
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.CLIENT_UNREACHABLE,
            };

            var result = _matchmakingService.RequestArrangedMatch(config);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void RequestArrangedMatch_CallbackThrowsException_ReturnsClientUnreachable()
        {
            var requesterId = Guid.NewGuid();
            var companionId = Guid.NewGuid();
            var requesterCallback = new Mock<IMatchmakingCallback>();
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
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.CLIENT_UNREACHABLE,
            };

            var result = _matchmakingService.RequestArrangedMatch(config);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ConfirmMatchReceived_BothPlayersConfirm_NotifiesPlayersReady()
        {
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

            _matchmakingService.ConfirmMatchReceived(requesterId, matchId);
            _matchmakingService.ConfirmMatchReceived(companionId, matchId);

            requesterCallback.Verify(c => c.NotifyPlayersReady(matchId), Times.Once);
            companionCallback.Verify(c => c.NotifyPlayersReady(matchId), Times.Once);
        }

        [Test]
        public void ConfirmMatchReceived_OnePlayerConfirms_DoesNotNotifyReadyYet()
        {
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

            _matchmakingService.ConfirmMatchReceived(requesterId, matchId);

            requesterCallback.Verify(c => c.NotifyPlayersReady(It.IsAny<Guid>()), Times.Never);
            companionCallback.Verify(c => c.NotifyPlayersReady(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void RequestMatchCancel_MatchPending_RemovesMatchAndNotifiesCompanion()
        {
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

            _matchmakingService.RequestMatchCancel(requesterId);

            companionCallback.Verify(c => c.NotifyMatchCanceled(matchId, StatusCode.CLIENT_CANCEL), Times.Once);
        }

        [Test]
        public void Disconnect_TheresPendingMatch_NotifiesCompanionMatchCanceled()
        {
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

            _matchmakingService.Disconnect(requesterId);

            companionCallback.Verify(c => c.NotifyMatchCanceled(matchId, StatusCode.CLIENT_CANCEL), Times.Once);
        }

        private void ConnectPlayer(Guid playerId, Mock<IMatchmakingCallback> callbackMock)
        {
            _callbackQueue.Enqueue(callbackMock.Object);
            _matchmakingService.Connect(playerId);
        }
    }
}