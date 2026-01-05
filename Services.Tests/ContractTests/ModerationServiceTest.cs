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
        private Mock<IPlayerDAO> _playerDaoMock;
        private ModerationService _moderationService;

        [SetUp]
        public void Setup()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
            _reportDaoMock = new Mock<IReportDAO>();
            _banDaoMock = new Mock<IBanDAO>();
            _playerDaoMock = new Mock<IPlayerDAO>();

            _moderationService = new ModerationService(
                _sessionManagerMock.Object,
                _reportDaoMock.Object,
                _banDaoMock.Object,
                _playerDaoMock.Object
            );
        }

        #region ReportPlayer Tests

        [Test]
        public void ReportPlayer_ReporterOffline_ReturnsUnauthorized()
        {
            // Arrange
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();

            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(false);

            // Act
            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Toxic behavior");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNAUTHORIZED.Equals(result.StatusCode));

            _playerDaoMock.Verify(p => p.GetPlayerById(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void ReportPlayer_ReporterNotFoundInDb_ReturnsNotFound()
        {
            // Arrange
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();

            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(true);

            _playerDaoMock.Setup(p => p.GetPlayerById(reporterId)).Returns((DataAccess.Player)null);

            // Act
            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Reason");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void ReportPlayer_ReportedTargetNotFoundInDb_ReturnsNotFound()
        {
            // Arrange
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();

            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterId)).Returns(true);

            _playerDaoMock.Setup(p => p.GetPlayerById(reporterId))
                .Returns(new DataAccess.Player { playerID = reporterId, userID = Guid.NewGuid() });

            _playerDaoMock.Setup(p => p.GetPlayerById(reportedId)).Returns((DataAccess.Player)null);

            // Act
            var result = _moderationService.ReportPlayer(reporterId, reportedId, "Reason");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void ReportPlayer_ReportAlreadyExists_ReturnsReportDuplicated()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();

            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);

            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId))
                .Returns(true);

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.REPORT_DUPLICATED.Equals(result.StatusCode));

            _reportDaoMock.Verify(r => r.AddReport(It.IsAny<Report>()), Times.Never);
        }

        [Test]
        public void ReportPlayer_ValidReport_NoBanThreshold_ReturnsReportCreated()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();

            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);

            _reportDaoMock.Setup(r => r.HasPlayerReportedTarget(reporterUserId, reportedUserId)).Returns(false);

            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(1);

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.REPORT_CREATED.Equals(result.StatusCode));

            _reportDaoMock.Verify(r => r.AddReport(It.IsAny<Report>()), Times.Once);
            _banDAO_VerifyBanApplied(Times.Never());
            _sessionManagerMock.Verify(s => s.KickPlayer(It.IsAny<Guid>(), It.IsAny<KickReason>()), Times.Never);
        }

        [Test]
        public void ReportPlayer_ThresholdReached_3Reports_Applies1HourBanAndKicks()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();

            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);

            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(3);

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Spam");

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.USER_KICKED_AND_BANNED.Equals(result.StatusCode));

            _banDaoMock.Verify(b => b.ApplyBan(It.Is<Ban>(ban =>
                ban.userID == reportedUserId &&
                ban.timeout > DateTimeOffset.Now.AddMinutes(50) &&
                ban.timeout < DateTimeOffset.Now.AddMinutes(70)
            )), Times.Once);

            _sessionManagerMock.Verify(s => s.KickPlayer(reportedPlayerId, KickReason.TEMPORARY_BAN), Times.Once);
        }

        [Test]
        public void ReportPlayer_ThresholdReached_10Reports_AppliesPermanentBanAndKicks()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            Guid reporterUserId = Guid.NewGuid();
            Guid reportedUserId = Guid.NewGuid();

            SetupPlayers(reporterPlayerId, reporterUserId, reportedPlayerId, reportedUserId);

            _reportDaoMock.Setup(r => r.CountUniqueReports(reportedUserId)).Returns(10);

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Severe insults");

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.USER_KICKED_AND_BANNED.Equals(result.StatusCode));

            _banDaoMock.Verify(b => b.ApplyBan(It.Is<Ban>(ban =>
                ban.userID == reportedUserId &&
                ban.timeout == DateTimeOffset.MaxValue
            )), Times.Once);

            _sessionManagerMock.Verify(s => s.KickPlayer(reportedPlayerId, KickReason.PERMANTENT_BAN), Times.Once);
        }

        [Test]
        public void ReportPlayer_DbUpdateException_ReturnsReportDuplicated()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, Guid.NewGuid(), reportedPlayerId, Guid.NewGuid());

            _reportDaoMock.Setup(r => r.AddReport(It.IsAny<Report>()))
                .Throws(new DbUpdateException());

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.REPORT_DUPLICATED.Equals(result.StatusCode));
        }

        [Test]
        public void ReportPlayer_GeneralException_ReturnsServerError()
        {
            // Arrange
            Guid reporterPlayerId = Guid.NewGuid();
            Guid reportedPlayerId = Guid.NewGuid();
            SetupPlayers(reporterPlayerId, Guid.NewGuid(), reportedPlayerId, Guid.NewGuid());

            _reportDaoMock.Setup(r => r.AddReport(It.IsAny<Report>()))
                .Throws(new Exception("Database connection lost"));

            // Act
            var result = _moderationService.ReportPlayer(reporterPlayerId, reportedPlayerId, "Reason");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        #endregion

        #region Helpers

        private void SetupPlayers(Guid reporterPlayerId, Guid reporterUserId, Guid reportedPlayerId, Guid reportedUserId)
        {
            _sessionManagerMock.Setup(s => s.IsPlayerOnline(reporterPlayerId)).Returns(true);

            _playerDaoMock.Setup(p => p.GetPlayerById(reporterPlayerId))
                .Returns(new DataAccess.Player { playerID = reporterPlayerId, userID = reporterUserId });

            _playerDaoMock.Setup(p => p.GetPlayerById(reportedPlayerId))
                .Returns(new DataAccess.Player { playerID = reportedPlayerId, userID = reportedUserId });
        }

        private void _banDAO_VerifyBanApplied(Times times)
        {
            _banDaoMock.Verify(b => b.ApplyBan(It.IsAny<Ban>()), times);
        }

        #endregion
    }
}