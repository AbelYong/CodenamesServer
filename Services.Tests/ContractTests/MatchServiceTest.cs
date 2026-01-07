using DataAccess.Scoreboards;
using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Match = Services.DTO.DataContract.Match;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class MatchServiceTest
    {
        private Queue<IMatchCallback> _callbackQueue;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private Mock<IScoreboardManager> _scoreboardManagerMock;
        private Mock<IScoreboardDAO> _scoreboardDAOMock;
        private MatchService _matchService;

        [SetUp]
        public void Setup()
        {
            _callbackProviderMock = new Mock<ICallbackProvider>();
            _scoreboardManagerMock = new Mock<IScoreboardManager>();
            _scoreboardDAOMock = new Mock<IScoreboardDAO>();
            _callbackQueue = new Queue<IMatchCallback>();

            _callbackProviderMock.Setup(cp => cp.GetCallback<IMatchCallback>())
                .Returns(() => _callbackQueue.Count > 0 ? _callbackQueue.Dequeue() : new Mock<IMatchCallback>().Object);

            _matchService = new MatchService(
                _callbackProviderMock.Object,
                _scoreboardManagerMock.Object,
                _scoreboardDAOMock.Object
            );
        }

        [Test]
        public void Connect_NewPlayer_ReturnsSuccess()
        {
            Guid playerId = Guid.NewGuid();
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            CommunicationRequest result = _matchService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void Connect_PlayerReconnects_ReturnsSuccess()
        {
            Guid playerId = Guid.NewGuid();
            _matchService.Connect(playerId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            CommunicationRequest result = _matchService.Connect(playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_MatchIsNull_ReturnsMissingData()
        {
            Match match = null;
            Guid playerId = Guid.NewGuid();
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            CommunicationRequest result = _matchService.JoinMatch(match, playerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_PlayerIsNotInMatch_ReturnsWrongData()
        {
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Guid otherPlayerId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.WRONG_DATA
            };

            CommunicationRequest result = _matchService.JoinMatch(match, otherPlayerId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_NewMatchRequesterJoins_ReturnsSuccessAndStartsMatch()
        {
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            CommunicationRequest result = _matchService.JoinMatch(match, requesterId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_NewMatchCompanionJoins_ReturnsSuccessAndStartsMatch()
        {
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            CommunicationRequest result = _matchService.JoinMatch(match, companionId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_ExistingMatchCompanionJoins_ReturnsSuccess()
        {
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);
            _matchService.JoinMatch(match, requesterId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK
            };

            CommunicationRequest result = _matchService.JoinMatch(match, companionId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void JoinMatch_PlayerAlreadyInMatch_ReturnsUnallowed()
        {
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Guid thirdPlayerID = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);
            _matchService.JoinMatch(match, companionId);
            Match secondMatch = CreateDummyMatch(thirdPlayerID, companionId);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED
            };

            CommunicationRequest result = _matchService.JoinMatch(secondMatch, companionId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SendClue_SpymasterSendsClue_GuesserReceivedCallback()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            string clue = "Test 2";
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _matchService.SendClue(spymasterId, clue);

            guesserCallback.Verify(cb => cb.NotifyClueReceived(clue), Times.Once);
        }

        [Test]
        public void SendClue_GuesserDisconnected_NotifiesSpymaster()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            string clue = "Test 2";
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            guesserCallback.Setup(cb => cb.NotifyClueReceived(clue))
                .Throws(new CommunicationException("Guesser connection failed"));

            _matchService.SendClue(spymasterId, clue);

            spymasterCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_SpymasterTimeout_NotifiesTurnChange()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _matchService.NotifyTurnTimeout(spymasterId, MatchRoleType.SPYMASTER);

            spymasterCallback.Verify(cb => cb.NotifyTurnChange(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyTurnChange(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeout_NotifiesSwitchRoles()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeoutNoTokens_MatchLost()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            Match match = CreateDummyMatch(spymasterId, guesserId);
            match.Rules.TimerTokens = 0;
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);

            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            spymasterCallback.Verify(cb => cb.NotifyMatchTimeout(It.IsAny<string>(), true), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyMatchTimeout(It.IsAny<string>(), true), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_SpymasterTimeoutGuesserOffline_NotifiesSpymaster()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            guesserCallback.Setup(cb => cb.NotifyTurnChange())
                .Throws(new CommunicationException("Guesser connection failed"));

            _matchService.NotifyTurnTimeout(spymasterId, MatchRoleType.SPYMASTER);

            spymasterCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeoutSpymasterOffline_NotifiesGuesser()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            spymasterCallback.Setup(cb => cb.NotifyGuesserTurnTimeout(It.IsAny<int>()))
                .Throws(new CommunicationException("Spymaster connection failed"));

            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_AgentsRemaining_NotifyAgentPicked()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AgentPickedNotification notification = new AgentPickedNotification
            {
                SenderID = guesserId,
                NewTurnLength = 30
            };

            _matchService.NotifyPickedAgent(notification);

            spymasterCallback.Verify(cb => cb.NotifyAgentPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyAgentPicked(notification), Times.Never);
        }

        [Test]
        public void NotifyPickedAgent_LastAgentPicked_NotifyMatchWonAndUpdatesScoreboard()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AgentPickedNotification notification = new AgentPickedNotification { SenderID = guesserId };

            int agentNumber = 15;
            for (int i = 0; i < agentNumber; i++)
            {
                _matchService.NotifyPickedAgent(notification);
            }

            spymasterCallback.Verify(cb => cb.NotifyMatchWon(It.IsAny<string>()), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyMatchWon(It.IsAny<string>()), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((spymasterId)), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((guesserId)), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_LastAgentPicked_NotifiesFailedScoreboardUpdate()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            _scoreboardDAOMock.Setup(dao => dao.UpdateMatchesWon(spymasterId))
                .Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateFastestMatchRecord(spymasterId, It.IsAny<TimeSpan>()))
                .Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateMatchesWon(guesserId))
                .Returns(false);

            AgentPickedNotification notification = new AgentPickedNotification { SenderID = guesserId };

            int agentNumber = 15;
            for (int i = 0; i < agentNumber; i++)
            {
                _matchService.NotifyPickedAgent(notification);
            }

            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((spymasterId)), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((guesserId)), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Never);
            guesserCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_SpymasterOffline_NotifiesGuesser()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AgentPickedNotification notification = new AgentPickedNotification
            {
                SenderID = guesserId,
                NewTurnLength = 30
            };
            spymasterCallback.Setup(cb => cb.NotifyAgentPicked(notification))
                .Throws(new CommunicationException("Spymaster connection failed"));

            _matchService.NotifyPickedAgent(notification);

            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_NormalMode_DecrementsTimerTokensNotifiesAndSwitchesRoles()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            Match match = CreateDummyMatch(spymasterId, guesserId);
            match.Rules.Gamemode = Gamemode.NORMAL;
            match.Rules.TimerTokens = 9;
            int initialTokens = match.Rules.TimerTokens;

            int remainingTokens = 0;
            TokenType updatedToken = TokenType.TEST;
            spymasterCallback
                .Setup(c => c.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()))
                .Callback<BystanderPickedNotification>(n =>
                {
                    updatedToken = n.TokenToUpdate;
                    remainingTokens = n.RemainingTokens;
                });

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);
            BystanderPickedNotification notification = new BystanderPickedNotification { SenderID = guesserId };
            int expectedTokens = initialTokens - MatchRules.TIMER_TOKENS_TO_TAKE_NON_CUSTOM;

            _matchService.NotifyPickedBystander(notification);

            Assert.That(remainingTokens.Equals(expectedTokens) && TokenType.TIMER.Equals(updatedToken));
            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);
            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_CustomMode_DecrementsTimerTokensNotifiesAndSwitchesRoles()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            Match match = CreateDummyMatch(spymasterId, guesserId);
            match.Rules.Gamemode = Gamemode.CUSTOM;
            match.Rules.BystanderTokens = 0;
            match.Rules.TimerTokens = 9;
            int initialTokens = match.Rules.TimerTokens;

            int remainingTokens = 0;
            TokenType updatedToken = TokenType.TEST;
            spymasterCallback
                .Setup(c => c.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()))
                .Callback<BystanderPickedNotification>(n =>
                {
                    updatedToken = n.TokenToUpdate;
                    remainingTokens = n.RemainingTokens;
                });

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);
            BystanderPickedNotification notification = new BystanderPickedNotification { SenderID = guesserId };
            int expectedTokens = initialTokens - MatchRules.TIMER_TOKENS_TO_TAKE_CUSTOM;

            _matchService.NotifyPickedBystander(notification);

            Assert.That(remainingTokens.Equals(expectedTokens) && TokenType.TIMER.Equals(updatedToken));
            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);
            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_CustomMode_DecrementsBystanderTokensNotifiesAndSwitchesRoles()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            Match match = CreateDummyMatch(spymasterId, guesserId);
            match.Rules.Gamemode = Gamemode.CUSTOM;
            match.Rules.BystanderTokens = 2;
            int initialTokens = match.Rules.BystanderTokens;

            int remainingTokens = 0;
            TokenType updatedToken = TokenType.TEST;
            spymasterCallback
                .Setup(c => c.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()))
                .Callback<BystanderPickedNotification>(n =>
                { 
                    updatedToken = n.TokenToUpdate;
                    remainingTokens = n.RemainingTokens;
                });

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);
            BystanderPickedNotification notification = new BystanderPickedNotification { SenderID = guesserId };
            int expectedTokens = initialTokens - 1;

            _matchService.NotifyPickedBystander(notification);

            Assert.That(remainingTokens.Equals(expectedTokens) && TokenType.BYSTANDER.Equals(updatedToken));
            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);
            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_SpymasterOffline_NotifiesGuesser()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            Match match = CreateDummyMatch(spymasterId, guesserId);

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);
            BystanderPickedNotification notification = new BystanderPickedNotification { SenderID = guesserId };
            spymasterCallback.Setup(cb => cb.NotifyBystanderPicked(notification))
                .Throws(new CommunicationException("Spymaster connection lost"));

            _matchService.NotifyPickedBystander(notification);

            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_Anytime_NotifiesAndUpdatesScoreboard()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };

            _matchService.NotifyPickedAssassin(notification);

            guesserCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_Anytime_ScoreboardUpdateFailsNotifiesStatsNotSaved()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };
            _scoreboardDAOMock.Setup(dao => dao.UpdateAssassinsPicked(spymasterId))
                .Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateAssassinsPicked(guesserId))
                .Returns(false);

            _matchService.NotifyPickedAssassin(notification);

            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Never);
        }

        [Test]
        public void NotifyPickedAssassin_SpymasterDisconnected_MatchEndsAndUpdatesScoreboard()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };
            spymasterCallback.Setup(cb => cb.NotifyAssassinPicked(notification))
                .Throws(new CommunicationException("Spymaster disconnected"));

            _matchService.NotifyPickedAssassin(notification);

            guesserCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_GuesserDisconnects_MatchEndsAndScoresSaved()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);
            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };
            guesserCallback.Setup(cb => cb.NotifyAssassinPicked(notification))
                .Throws(new CommunicationException("Gusser disconnected"));

            _matchService.NotifyPickedAssassin(notification);

            spymasterCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void Disconnect_GuesserInMatch_NotifiesSpymaster()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _matchService.Disconnect(spymasterId);

            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void Disconnect_SpymasterInMatch_NotifiesGuesser()
        {
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _matchService.Disconnect(guesserId);

            spymasterCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        private void InitializeActiveMatch(Guid spymasterId, Guid guesserId)
        {
            Match match = CreateDummyMatch(spymasterId, guesserId);
            _matchService.JoinMatch(match, spymasterId);
            _matchService.JoinMatch(match, guesserId);
        }

        private void ConnectPlayer(Guid playerId, Mock<IMatchCallback> callbackMock)
        {
            _callbackQueue.Enqueue(callbackMock.Object);
            _matchService.Connect(playerId);
        }

        private static Match CreateDummyMatch(Guid requesterId, Guid companionId)
        {
            return new Match
            {
                MatchID = Guid.NewGuid(),
                Requester = new Player { PlayerID = requesterId },
                Companion = new Player { PlayerID = companionId },
                Rules = new MatchRules
                {
                    Gamemode = Gamemode.NORMAL,
                    TimerTokens = MatchRules.NORMAL_TIMER_TOKENS,
                    BystanderTokens = MatchRules.NORMAL_BYSTANDER_TOKENS
                }
            };
        }
    }
}
