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
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act
            CommunicationRequest result = _matchService.Connect(playerId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void Connect_PlayerReconnects_ReturnsSuccess()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            _matchService.Connect(playerId); // First connection

            // Act
            CommunicationRequest result = _matchService.Connect(playerId); // Reconnection

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_MatchIsNull_ReturnsMissingData()
        {
            // Arrange
            Match match = null;
            Guid playerId = Guid.NewGuid();

            // Act
            CommunicationRequest result = _matchService.JoinMatch(match, playerId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_PlayerIsNotInMatch_ReturnsWrongData()
        {
            // Arrange
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Guid otherPlayerId = Guid.NewGuid();

            Match match = CreateDummyMatch(requesterId, companionId);

            // Act
            CommunicationRequest result = _matchService.JoinMatch(match, otherPlayerId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.WRONG_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_NewMatchRequesterJoins_ReturnsSuccessAndStartsMatch()
        {
            // Arrange
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);

            // Act
            CommunicationRequest result = _matchService.JoinMatch(match, requesterId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_NewMatchCompanionJoins_ReturnsSuccessAndStartsMatch()
        {
            // Arrange
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);

            // Act
            CommunicationRequest result = _matchService.JoinMatch(match, companionId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_ExistingMatchCompanionJoins_ReturnsSuccess()
        {
            // Arrange
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Match match = CreateDummyMatch(requesterId, companionId);

            // First start the match with requester
            _matchService.JoinMatch(match, requesterId);

            // Act
            CommunicationRequest result = _matchService.JoinMatch(match, companionId);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.OK.Equals(result.StatusCode));
        }

        [Test]
        public void JoinMatch_PlayerAlreadyInMatch_ReturnsUnallowed()
        {
            // Arrange
            Guid requesterId = Guid.NewGuid();
            Guid companionId = Guid.NewGuid();
            Guid thirdPlayerID = Guid.NewGuid();

            Match match = CreateDummyMatch(requesterId, companionId);
            _matchService.JoinMatch(match, companionId); // Join once

            // Act
            Match secondMatch = CreateDummyMatch(thirdPlayerID, companionId);
            CommunicationRequest result = _matchService.JoinMatch(secondMatch, companionId); // Try join another match

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
        }

        [Test]
        public void SendClue_SpymasterSendsClue_GuesserReceivedCallback()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();
            string clue = "Test 2";

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            // Act
            _matchService.SendClue(spymasterId, clue);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyClueReceived(clue), Times.Once);
        }

        [Test]
        public void SendClue_GuesserDisconnected_NotifiesSpymaster()
        {
            // Arrange
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

            // Act
            _matchService.SendClue(spymasterId, clue);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_SpymasterTimeout_NotifiesTurnChange()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            // Act
            _matchService.NotifyTurnTimeout(spymasterId, MatchRoleType.SPYMASTER);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyTurnChange(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyTurnChange(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeout_NotifiesSwitchRoles()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            // Act
            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeoutNoTokens_MatchLost()
        {
            // Arrange
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

            // Act
            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyMatchTimeout(It.IsAny<string>(), true), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyMatchTimeout(It.IsAny<string>(), true), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_SpymasterTimeoutGuesserOffline_NotifiesSpymaster()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            guesserCallback.Setup(cb => cb.NotifyTurnChange())
                .Throws(new CommunicationException("Guesser connection failed"));

            // Act
            _matchService.NotifyTurnTimeout(spymasterId, MatchRoleType.SPYMASTER);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyTurnTimeout_GuesserTimeoutSpymasterOffline_NotifiesGuesser()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            spymasterCallback.Setup(cb => cb.NotifyGuesserTurnTimeout(It.IsAny<int>()))
                .Throws(new CommunicationException("Spymaster connection failed"));

            // Act
            _matchService.NotifyTurnTimeout(guesserId, MatchRoleType.GUESSER);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_AgentAvailable_NotifyAgentPicked()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedAgent(notification);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyAgentPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyAgentPicked(notification), Times.Never);
        }

        [Test]
        public void NotifyPickedAgent_LastAgentPicked_NotifyMatchWonAndUpdatesScoreboard()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            AgentPickedNotification notification = new AgentPickedNotification { SenderID = guesserId };

            // Act
            int agentNumber = 15;
            for (int i = 0; i < agentNumber; i++)
            {
                _matchService.NotifyPickedAgent(notification);
            }

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyMatchWon(It.IsAny<string>()), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyMatchWon(It.IsAny<string>()), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((spymasterId)), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((guesserId)), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_LastAgentPicked_NotifiesFailedScoreboardUpdate()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            _scoreboardDAOMock.Setup(dao => dao.UpdateMatchesWon(spymasterId)).Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateFastestMatchRecord(spymasterId, It.IsAny<TimeSpan>())).Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateMatchesWon(guesserId)).Returns(false);

            AgentPickedNotification notification = new AgentPickedNotification { SenderID = guesserId };

            // Act
            int agentNumber = 15;
            for (int i = 0; i < agentNumber; i++)
            {
                _matchService.NotifyPickedAgent(notification);
            }

            // Assert
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((spymasterId)), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateMatchesWon((guesserId)), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Never);
            guesserCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Once);
        }

        [Test]
        public void NotifyPickedAgent_SpymasterOffline_NotifiesGuesser()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedAgent(notification);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_NormalMode_DecrementsTimerTokensAndSwitchesRoles()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedBystander(notification);

            // Assert
            Assert.That(remainingTokens.Equals(expectedTokens));
            Assert.That(TokenType.TIMER.Equals(updatedToken));

            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);

            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_CustomMode_DecrementsBystanderTokensAndSwitchesRoles()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedBystander(notification);

            // Assert
            Assert.That(remainingTokens.Equals(expectedTokens));
            Assert.That(TokenType.BYSTANDER.Equals(updatedToken));

            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);

            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_CustomMode_DecrementsTimerTokensAndSwitchesRoles()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedBystander(notification);

            // Assert
            Assert.That(remainingTokens.Equals(expectedTokens));
            Assert.That(TokenType.TIMER.Equals(updatedToken));

            spymasterCallback.Verify(cb => cb.NotifyBystanderPicked(notification), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyBystanderPicked(It.IsAny<BystanderPickedNotification>()), Times.Never);

            spymasterCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyRolesChanged(), Times.Once);
        }

        [Test]
        public void NotifyPickedBystander_SpymasterOffline_NotifiesGuesser()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedBystander(notification);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_Anytime_MatchEndsAndNotificationsSent()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };

            // Act
            _matchService.NotifyPickedAssassin(notification);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_Anytime_MatchEndsScoreboardUpdateFailed()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            AssassinPickedNotification notification = new AssassinPickedNotification { SenderID = guesserId };

            _scoreboardDAOMock.Setup(dao => dao.UpdateAssassinsPicked(spymasterId)).Returns(true);
            _scoreboardDAOMock.Setup(dao => dao.UpdateAssassinsPicked(guesserId)).Returns(false);

            // Act
            _matchService.NotifyPickedAssassin(notification);

            // Assert
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
            guesserCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Once);
            spymasterCallback.Verify(cb => cb.NotifyStatsCouldNotBeSaved(), Times.Never);
        }

        [Test]
        public void NotifyPickedAssassin_SpymasterDisconnect_MatchEndsAndScoresSaved()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedAssassin(notification);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void NotifyPickedAssassin_GuesserDisconnects_MatchEndsAndScoresSaved()
        {
            // Arrange
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

            // Act
            _matchService.NotifyPickedAssassin(notification);

            // Assert
            spymasterCallback.Verify(cb => cb.NotifyAssassinPicked(notification), Times.Once);
            _scoreboardDAOMock.Verify(dao => dao.UpdateAssassinsPicked(guesserId), Times.Once);
        }

        [Test]
        public void Disconnect_GuesserInMatch_NotifiesSpymaster()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            // Act
            _matchService.Disconnect(spymasterId);

            // Assert
            guesserCallback.Verify(cb => cb.NotifyCompanionDisconnect(), Times.Once);
        }

        [Test]
        public void Disconnect_SpymasterInMatch_NotifiesGuesser()
        {
            // Arrange
            Guid spymasterId = Guid.NewGuid();
            Guid guesserId = Guid.NewGuid();
            Mock<IMatchCallback> spymasterCallback = new Mock<IMatchCallback>();
            Mock<IMatchCallback> guesserCallback = new Mock<IMatchCallback>();

            ConnectPlayer(spymasterId, spymasterCallback);
            ConnectPlayer(guesserId, guesserCallback);
            InitializeActiveMatch(spymasterId, guesserId);

            // Act
            _matchService.Disconnect(guesserId);

            // Assert
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
