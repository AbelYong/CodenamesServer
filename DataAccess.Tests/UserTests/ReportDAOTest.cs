using DataAccess.Test;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class ReportDAOTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Report>> _reportSet;
        private ReportDAO _reportDAO;
        private List<Report> _reportsData;

        [SetUp]
        public void Setup()
        {
            _reportsData = new List<Report>();
            _reportSet = TestUtil.GetQueryableMockDbSet(_reportsData);

            _context = new Mock<ICodenamesContext>();
            _context.Setup(c => c.Reports).Returns(_reportSet.Object);

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _reportDAO = new ReportDAO(_contextFactory.Object);
        }

        #region HasPlayerReportedTarget

        [Test]
        public void HasPlayerReportedTarget_ReportExists_ReturnsTrue()
        {
            // Arrange
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _reportsData.Add(new Report { reporterUserID = reporterId, reportedUserID = reportedId });

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(reporterId, reportedId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasPlayerReportedTarget_ReportDoesNotExist_ReturnsFalse()
        {
            // Arrange
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _reportsData.Add(new Report { reporterUserID = reporterId, reportedUserID = Guid.NewGuid() });

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(reporterId, reportedId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_SqlException_ReturnsFalse()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(SqlExceptionCreator.Create());

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_EntityException_ReturnsFalse()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new EntityException());

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_TimeoutException_ReturnsFalse()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new TimeoutException());

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_GeneralException_ReturnsFalse()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new Exception());

            // Act
            bool result = _reportDAO.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region AddReport

        [Test]
        public void AddReport_ValidReport_AddsAndSavesChanges()
        {
            // Arrange
            var report = new Report { reportID = Guid.NewGuid() };

            // Act
            _reportDAO.AddReport(report);

            // Assert
            _reportSet.Verify(m => m.Add(report), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void AddReport_DbUpdateException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            // Act & Assert
            Assert.Throws<DbUpdateException>(() => _reportDAO.AddReport(new Report()));
        }

        [Test]
        public void AddReport_SqlException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(SqlExceptionCreator.Create());

            // Act & Assert
            Assert.Throws<SqlException>(() => _reportDAO.AddReport(new Report()));
        }

        [Test]
        public void AddReport_EntityException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            // Act & Assert
            Assert.Throws<EntityException>(() => _reportDAO.AddReport(new Report()));
        }

        [Test]
        public void AddReport_TimeoutException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new TimeoutException());

            // Act & Assert
            Assert.Throws<TimeoutException>(() => _reportDAO.AddReport(new Report()));
        }

        [Test]
        public void AddReport_GeneralException_RethrowsException()
        {
            // Arrange
            _context.Setup(c => c.SaveChanges()).Throws(new Exception());

            // Act & Assert
            Assert.Throws<Exception>(() => _reportDAO.AddReport(new Report()));
        }

        #endregion

        #region CountUniqueReports

        [Test]
        public void CountUniqueReports_ReportsExist_ReturnsCorrectUniqueCount()
        {
            // Arrange
            Guid targetUser = Guid.NewGuid();
            Guid reporterA = Guid.NewGuid();
            Guid reporterB = Guid.NewGuid();

            _reportsData.Add(new Report { reporterUserID = reporterA, reportedUserID = targetUser });
            _reportsData.Add(new Report { reporterUserID = reporterA, reportedUserID = targetUser });
            _reportsData.Add(new Report { reporterUserID = reporterB, reportedUserID = targetUser });
            _reportsData.Add(new Report { reporterUserID = reporterA, reportedUserID = Guid.NewGuid() });

            // Act
            int count = _reportDAO.CountUniqueReports(targetUser);

            // Assert
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void CountUniqueReports_NoReports_ReturnsZero()
        {
            // Arrange
            Guid targetUser = Guid.NewGuid();

            // Act
            int count = _reportDAO.CountUniqueReports(targetUser);

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_SqlException_ReturnsZero()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(SqlExceptionCreator.Create());

            // Act
            int count = _reportDAO.CountUniqueReports(Guid.NewGuid());

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_EntityException_ReturnsZero()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new EntityException());

            // Act
            int count = _reportDAO.CountUniqueReports(Guid.NewGuid());

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_TimeoutException_ReturnsZero()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new TimeoutException());

            // Act
            int count = _reportDAO.CountUniqueReports(Guid.NewGuid());

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_GeneralException_ReturnsZero()
        {
            // Arrange
            _context.Setup(c => c.Reports).Throws(new Exception());

            // Act
            int count = _reportDAO.CountUniqueReports(Guid.NewGuid());

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        #endregion
    }
}