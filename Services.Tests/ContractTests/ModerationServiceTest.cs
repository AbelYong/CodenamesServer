using DataAccess;
using DataAccess.Moderation;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using Services.Contracts.ServiceContracts.Managers;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Data;
using System.Data.Entity.Infrastructure;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class ModerationServiceTest
    {
        private Mock<ISessionManager> _sessionManagerMock;
        private Mock<IReportDAO> _reportDaoMock;
        private Mock<IBanDAO> _banDaoMock;
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private ModerationService _moderationService;

        [SetUp]
        public void Setup()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
            _reportDaoMock = new Mock<IReportDAO>();
            _banDaoMock = new Mock<IBanDAO>();
            _playerRepositoryMock = new Mock<IPlayerRepository>();

            _moderationService = new ModerationService(
                _sessionManagerMock.Object,
                _reportDaoMock.Object,
                _banDaoMock.Object,
                _playerRepositoryMock.Object
            );
        }

        [Test]
        public void ReportPlayer_ReporterOffline_ReturnsUnauthorized()
        {
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(false);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNAUTHORIZED
            };

            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Toxic behavior");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ReportPlayer_ReporterNotFoundInDb_ReturnsNotFound()
        {
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(true);
            _playerRepositoryMock.Setup(p => p.GetPlayerById(reporterId)).Returns((DataAccess.Player)null);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Reason");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ReportPlayer_ReportedTargetNotFoundInDb_ReturnsNotFound()
        {
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(true);
            _playerRepositoryMock.Setup(p => p.GetPlayerById(reporterId))
                .Returns(new DataAccess.Player { playerID = reporterId, userID = Guid.NewGuid() });
            _playerRepositoryMock.Setup(p => p.GetPlayerById(reportedId)).Returns((DataAccess.Player)null);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Reason");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ReportPlayer_ReportAlreadyExists_ReturnsReportDuplicatedReportNotAdded()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);
            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId))
                .Returns(true);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.REPORT_DUPLICATED
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            Assert.That(result.Equals(expected));
            _reportDaoMock.Verify(r => r.AddReport(It.IsAny<Report>()), Times.Never);
        }

        [Test]
        public void ReportPlayer_ValidReport_NoBanThreshold_ReturnsReportCreatedBanNotAppliedTargetNotKicked()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);
            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId)).Returns(false);
            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(1);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.REPORT_CREATED
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            Assert.That(result.Equals(expected));
            _reportDaoMock.Verify(r => r.AddReport(It.IsAny<Report>()), Times.Once);
            _banDaoMock.Verify(b => b.ApplyBan(It.IsAny<Ban>()), Times.Never);
            _sessionManagerMock.Verify(s => s.KickPlayer(It.IsAny<Guid>(), It.IsAny<KickReason>()), Times.Never);
        }

        [Test]
        public void ReportPlayer_ThresholdReached_3Reports_ReturnsUserKickedApplies1HourBanAndKicks()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);
            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId)).Returns(false);
            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(3);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.USER_KICKED_AND_BANNED
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Spam");

            Assert.That(result.Equals(expected));
            _banDaoMock.Verify(b => b.ApplyBan(It.Is<Ban>(ban =>
                ban.userID == reportedUserId &&
                ban.timeout > DateTimeOffset.Now.AddMinutes(50) &&
                ban.timeout < DateTimeOffset.Now.AddMinutes(70)
            )), Times.Once);
            _sessionManagerMock.Verify(s => s.KickPlayer(reportedPlayerId, KickReason.TEMPORARY_BAN), Times.Once);
        }

        [Test]
        public void ReportPlayer_ThresholdReached_10Reports_ReturnsUserKickedAppliesPermanentBanAndKicks()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);
            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId)).Returns(false);
            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(10);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.USER_KICKED_AND_BANNED
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Severe insults");

            Assert.That(result.Equals(expected));
            _banDaoMock.Verify(b => b.ApplyBan(It.Is<Ban>(ban =>
                ban.userID == reportedUserId &&
                ban.timeout == DateTimeOffset.MaxValue
            )), Times.Once);
            _sessionManagerMock.Verify(s => s.KickPlayer(reportedPlayerId, KickReason.PERMANTENT_BAN), Times.Once);
        }

        [Test]
        public void ReportPlayer_DbUpdateException_ReturnsReportDuplicated()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, Guid.NewGuid(), reportedPlayerId, Guid.NewGuid());
            _reportDaoMock.Setup(r => r.AddReport(It.IsAny<Report>()))
                .Throws(new DbUpdateException());
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.REPORT_DUPLICATED
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void ReportPlayer_GeneralException_ReturnsServerError()
        {
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, Guid.NewGuid(), reportedPlayerId, Guid.NewGuid());
            _reportDaoMock.Setup(r => r.AddReport(It.IsAny<Report>()))
                .Throws(new Exception("Database connection lost"));
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            Assert.That(result.Equals(expected));
        }

        private void SetupPlayers(Guid reporterPlayerId, Guid reporterUserId, Guid reportedPlayerId, Guid reportedUserId)
        {
            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterPlayerId)).Returns(true);

            _playerRepositoryMock.Setup(p => p.GetPlayerById(reporterPlayerId))
                .Returns(new DataAccess.Player { playerID = reporterPlayerId, userID = reporterUserId });

            _playerRepositoryMock.Setup(p => p.GetPlayerById(reportedPlayerId))
                .Returns(new DataAccess.Player { playerID = reportedPlayerId, userID = reportedUserId });
        }
    }
}