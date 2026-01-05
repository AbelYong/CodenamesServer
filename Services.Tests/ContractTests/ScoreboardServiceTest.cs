using DataAccess.Scoreboards;
using Moq;
using NUnit.Framework;
using Services.Contracts;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class ScoreboardServiceTest
    {
        private Mock<IScoreboardDAO> _scoreboardDaoMock;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private ScoreboardService _scoreboardService;

        [SetUp]
        public void Setup()
        {
            _scoreboardDaoMock = new Mock<IScoreboardDAO>();
            _callbackProviderMock = new Mock<ICallbackProvider>();

            _scoreboardService = new ScoreboardService(
                _scoreboardDaoMock.Object,
                _callbackProviderMock.Object
            );
        }

        [Test]
        public void GetMyScore_PlayerExists_ReturnsMappedScoreboard()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var dbScoreboard = new DataAccess.Scoreboard
            {
                Player = new DataAccess.Player { username = "TestUser" },
                mostGamesWon = 10,
                fastestGame = TimeSpan.FromMinutes(2),
                assassinsRevealed = 5
            };

            _scoreboardDaoMock.Setup(d => d.GetPlayerScoreboard(playerId))
                .Returns(dbScoreboard);

            // Act
            var result = _scoreboardService.GetMyScore(playerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Username, Is.EqualTo("TestUser"));
            Assert.That(result.GamesWon, Is.EqualTo(10));
            Assert.That(result.FastestMatch, Is.EqualTo("02:00"));
        }

        [Test]
        public void GetMyScore_PlayerNotFound_ReturnsNull()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            _scoreboardDaoMock.Setup(d => d.GetPlayerScoreboard(playerId))
                .Returns((DataAccess.Scoreboard)null);

            // Act
            var result = _scoreboardService.GetMyScore(playerId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetMyScore_DbPlayerNull_ReturnsUnknownUsername()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var dbScoreboard = new DataAccess.Scoreboard
            {
                Player = null,
                mostGamesWon = 0
            };

            _scoreboardDaoMock.Setup(d => d.GetPlayerScoreboard(playerId))
                .Returns(dbScoreboard);

            // Act
            var result = _scoreboardService.GetMyScore(playerId);

            // Assert
            Assert.That(result.Username, Is.EqualTo("Unknown"));
        }

        [Test]
        public void Subscribe_NewPlayer_AddsToDictionaryAndSendsUpdate()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>())
                .Returns(callbackMock.Object);

            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(10))
                .Returns(new List<DataAccess.Scoreboard>());

            // Act
            _scoreboardService.SubscribeToScoreboardUpdates(playerId);

            // Assert
            callbackMock.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Once);
        }

        [Test]
        public void Subscribe_CallbackError_LogsWarningAndDoesNotThrow()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>())
                .Returns(callbackMock.Object);

            callbackMock.Setup(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()))
                .Throws(new CommunicationException("Connection fail"));

            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(10))
                .Returns(new List<DataAccess.Scoreboard>());

            // Act & Assert
            Assert.DoesNotThrow(() => _scoreboardService.SubscribeToScoreboardUpdates(playerId));
        }

        [Test]
        public void Subscribe_UnexpectedException_LogsErrorAndDoesNotThrow()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>())
                .Throws(new Exception("Unexpected error getting callback"));

            // Act & Assert
            Assert.DoesNotThrow(() => _scoreboardService.SubscribeToScoreboardUpdates(playerId));
        }

        [Test]
        public void Unsubscribe_ExistingPlayer_RemovesAndClosesChannel()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>())
                .Returns(callbackMock.Object);
            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(It.IsAny<int>()))
                .Returns(new List<DataAccess.Scoreboard>());

            _scoreboardService.SubscribeToScoreboardUpdates(playerId);

            // Act
            _scoreboardService.UnsubscribeFromScoreboardUpdates(playerId);

            // Assert
            callbackMock.As<ICommunicationObject>().Verify(co => co.Close(), Times.Once);
        }

        [Test]
        public void Unsubscribe_NonExistentPlayer_DoesNothing()
        {
            // Arrange
            Guid playerId = Guid.NewGuid();

            // Act
            _scoreboardService.UnsubscribeFromScoreboardUpdates(playerId);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void NotifyMatchConcluded_ClientsConnected_NotifiesAllOpenedClients()
        {
            // Arrange
            Guid player1 = Guid.NewGuid();
            Guid player2 = Guid.NewGuid();

            var callback1 = CreateMockCallback(CommunicationState.Opened);
            var callback2 = CreateMockCallback(CommunicationState.Opened);

            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(10))
                .Returns(new List<DataAccess.Scoreboard>());

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback1.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback2.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player2);

            // Act
            _scoreboardService.NotifyMatchConcluded();

            // Assert
            callback1.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Exactly(2));
            callback2.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Exactly(2));
        }

        [Test]
        public void NotifyMatchConcluded_ClientClosed_RemovesClient()
        {
            // Arrange
            Guid player1 = Guid.NewGuid();
            var callback1 = CreateMockCallback(CommunicationState.Closed);

            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(10))
                .Returns(new List<DataAccess.Scoreboard>());

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback1.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);

            // Act
            _scoreboardService.NotifyMatchConcluded();

            // Assert
            callback1.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Once);
        }

        [Test]
        public void NotifyMatchConcluded_ClientFaulted_AbortsConnection()
        {
            // Arrange
            Guid player1 = Guid.NewGuid();
            var callback1 = CreateMockCallback(CommunicationState.Opened);

            _scoreboardDaoMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(new List<DataAccess.Scoreboard>());

            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback1.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);

            callback1.Setup(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()))
                .Throws(new CommunicationException("Network drop"));

            // Act
            _scoreboardService.NotifyMatchConcluded();

            // Assert
            callback1.As<ICommunicationObject>().Verify(co => co.Abort(), Times.Once);
        }

        private static Mock<IScoreboardCallback> CreateMockCallback(CommunicationState state)
        {
            var mock = new Mock<IScoreboardCallback>();
            var commMock = mock.As<ICommunicationObject>();
            commMock.Setup(c => c.State).Returns(state);
            return mock;
        }
    }
}