using DataAccess.DataRequests;
using DataAccess.Scoreboards;
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
    public class ScoreboardServiceTest
    {
        private Mock<IScoreboardRepository> _scoreboardRepositoryMock;
        private Mock<ICallbackProvider> _callbackProviderMock;
        private ScoreboardService _scoreboardService;

        [SetUp]
        public void Setup()
        {
            _scoreboardRepositoryMock = new Mock<IScoreboardRepository>();
            _callbackProviderMock = new Mock<ICallbackProvider>();

            _scoreboardService = new ScoreboardService(
                _scoreboardRepositoryMock.Object,
                _callbackProviderMock.Object
            );
        }

        [Test]
        public void GetMyScore_PlayerExists_ReturnsCorrectUsername()
        {
            Guid playerId = Guid.NewGuid();
            var dbScoreboard = new DataAccess.Scoreboard
            {
                Player = new DataAccess.Player { username = "TestUser" },
                mostGamesWon = 10,
                fastestGame = TimeSpan.FromMinutes(2),
                assassinsRevealed = 5
            };
            _scoreboardRepositoryMock.Setup(d => d.GetPlayerScoreboard(playerId)).Returns(dbScoreboard);

            var result = _scoreboardService.GetMyScore(playerId);

            Assert.That(result.Username, Is.EqualTo("TestUser"));
        }

        [Test]
        public void GetMyScore_PlayerExists_ReturnsCorrectGamesWon()
        {
            Guid playerId = Guid.NewGuid();
            var dbScoreboard = new DataAccess.Scoreboard
            {
                Player = new DataAccess.Player { username = "TestUser" },
                mostGamesWon = 10
            };
            _scoreboardRepositoryMock.Setup(d => d.GetPlayerScoreboard(playerId)).Returns(dbScoreboard);

            var result = _scoreboardService.GetMyScore(playerId);

            Assert.That(result.GamesWon, Is.EqualTo(10));
        }

        [Test]
        public void GetMyScore_PlayerNotFound_ReturnsNull()
        {
            Guid playerId = Guid.NewGuid();
            _scoreboardRepositoryMock.Setup(d => d.GetPlayerScoreboard(playerId)).Returns((DataAccess.Scoreboard)null);

            var result = _scoreboardService.GetMyScore(playerId);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetMyScore_PlayerReferenceNull_ReturnsUnknownUsername()
        {
            Guid playerId = Guid.NewGuid();
            var dbScoreboard = new DataAccess.Scoreboard { Player = null };
            _scoreboardRepositoryMock.Setup(d => d.GetPlayerScoreboard(playerId)).Returns(dbScoreboard);

            var result = _scoreboardService.GetMyScore(playerId);

            Assert.That(result.Username, Is.EqualTo("Unknown"));
        }

        [Test]
        public void GetTopPlayers_Success_ReturnsPopulatedList()
        {
            var repositoryResponse = new ScoreboardListRequest
            {
                IsSuccess = true,
                Scoreboards = new List<DataAccess.Scoreboard> { new DataAccess.Scoreboard { Player = new DataAccess.Player() } }
            };
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);

            var result = _scoreboardService.GetTopPlayers();

            Assert.That(result.ScoreboardList.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetTopPlayers_DbError_ReturnsDatabaseError()
        {
            var repositoryResponse = new ScoreboardListRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DB_ERROR
            };
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);

            var result = _scoreboardService.GetTopPlayers();

            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.DATABASE_ERROR));
        }

        [Test]
        public void SubscribeToScoreboardUpdates_ValidPlayer_SendsInitialUpdate()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);
            var repositoryResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callbackMock.Object);
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);

            _scoreboardService.SubscribeToScoreboardUpdates(playerId);

            callbackMock.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Once);
        }

        [Test]
        public void SubscribeToScoreboardUpdates_CommunicationException_DoesNotThrow()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);
            var repositoryResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callbackMock.Object);
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);
            callbackMock.Setup(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>())).Throws(new CommunicationException());

            Assert.DoesNotThrow(() => _scoreboardService.SubscribeToScoreboardUpdates(playerId));
        }

        [Test]
        public void SubscribeToScoreboardUpdates_GeneralException_DoesNotThrow()
        {
            Guid playerId = Guid.NewGuid();
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Throws(new Exception());

            Assert.DoesNotThrow(() => _scoreboardService.SubscribeToScoreboardUpdates(playerId));
        }

        [Test]
        public void UnsubscribeFromScoreboardUpdates_ExistingPlayer_ClosesChannel()
        {
            Guid playerId = Guid.NewGuid();
            var callbackMock = CreateMockCallback(CommunicationState.Opened);
            var repositoryResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callbackMock.Object);
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);
            _scoreboardService.SubscribeToScoreboardUpdates(playerId);

            _scoreboardService.UnsubscribeFromScoreboardUpdates(playerId);

            callbackMock.As<ICommunicationObject>().Verify(co => co.Close(), Times.Once);
        }

        [Test]
        public void UnsubscribeFromScoreboardUpdates_NonExistentPlayer_DoesNotThrow()
        {
            Guid playerId = Guid.NewGuid();

            Assert.DoesNotThrow(() => _scoreboardService.UnsubscribeFromScoreboardUpdates(playerId));
        }

        [Test]
        public void NotifyMatchConcluded_ClientsConnected_NotifiesAll()
        {
            Guid player1 = Guid.NewGuid();
            Guid player2 = Guid.NewGuid();
            var callback1 = CreateMockCallback(CommunicationState.Opened);
            var callback2 = CreateMockCallback(CommunicationState.Opened);
            var repositoryResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);
            _callbackProviderMock.SetupSequence(cp => cp.GetCallback<IScoreboardCallback>())
                .Returns(callback1.Object)
                .Returns(callback2.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);
            _scoreboardService.SubscribeToScoreboardUpdates(player2);

            _scoreboardService.NotifyMatchConcluded();

            callback1.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Exactly(2));
            callback2.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Exactly(2));
        }

        [Test]
        public void NotifyMatchConcluded_ClientFaulted_AbortsConnection()
        {
            Guid player1 = Guid.NewGuid();
            var callback1 = CreateMockCallback(CommunicationState.Opened);
            var repositoryResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            _scoreboardRepositoryMock.Setup(d => d.GetTopPlayersByWins(10)).Returns(repositoryResponse);
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback1.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);
            callback1.Setup(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>())).Throws(new CommunicationException());

            _scoreboardService.NotifyMatchConcluded();

            callback1.As<ICommunicationObject>().Verify(co => co.Abort(), Times.Once);
        }

        [Test]
        public void NotifyMatchConcluded_DbError_DoesNotNotify()
        {
            Guid player1 = Guid.NewGuid();
            var callback1 = CreateMockCallback(CommunicationState.Opened);
            var successResponse = new ScoreboardListRequest { IsSuccess = true, Scoreboards = new List<DataAccess.Scoreboard>() };
            var errorResponse = new ScoreboardListRequest { IsSuccess = false, ErrorType = ErrorType.DB_ERROR };
            _scoreboardRepositoryMock.SetupSequence(d => d.GetTopPlayersByWins(10))
                .Returns(successResponse)
                .Returns(errorResponse);
            _callbackProviderMock.Setup(cp => cp.GetCallback<IScoreboardCallback>()).Returns(callback1.Object);
            _scoreboardService.SubscribeToScoreboardUpdates(player1);

            _scoreboardService.NotifyMatchConcluded();

            callback1.Verify(cb => cb.NotifyLeaderboardUpdate(It.IsAny<List<Scoreboard>>()), Times.Once);
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