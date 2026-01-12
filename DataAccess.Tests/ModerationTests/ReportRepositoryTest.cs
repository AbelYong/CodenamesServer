using DataAccess.Moderation;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using DataAccess.Tests.Util;

namespace DataAccess.Tests.ModerationTests
{
    [TestFixture]
    public class ReportRepositoryTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<DbSet<Report>> _reportSet;
        private ReportRepository _reportRepository;
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

            _reportRepository = new ReportRepository(_contextFactory.Object);
        }

        [Test]
        public void HasPlayerReportedTarget_ReportExists_ReturnsTrue()
        {
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _reportsData.Add(new Report { reporterUserID = reporterId, reportedUserID = reportedId });

            bool result = _reportRepository.HasPlayerReportedTarget(reporterId, reportedId);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasPlayerReportedTarget_ReportDoesNotExist_ReturnsFalse()
        {
            Guid reporterId = Guid.NewGuid();
            Guid reportedId = Guid.NewGuid();
            _reportsData.Add(new Report { reporterUserID = reporterId, reportedUserID = Guid.NewGuid() });

            bool result = _reportRepository.HasPlayerReportedTarget(reporterId, reportedId);

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_SqlException_ReturnsFalse()
        {
            _context.Setup(c => c.Reports).Throws(SqlExceptionCreator.Create());

            bool result = _reportRepository.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_EntityException_ReturnsFalse()
        {
            _context.Setup(c => c.Reports).Throws(new EntityException());

            bool result = _reportRepository.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_TimeoutException_ReturnsFalse()
        {
            _context.Setup(c => c.Reports).Throws(new TimeoutException());

            bool result = _reportRepository.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlayerReportedTarget_GeneralException_ReturnsFalse()
        {
            _context.Setup(c => c.Reports).Throws(new Exception());

            bool result = _reportRepository.HasPlayerReportedTarget(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void AddReport_ValidReport_AddsAndSavesChanges()
        {
            var report = new Report { reportID = Guid.NewGuid() };

            _reportRepository.AddReport(report);

            _reportSet.Verify(m => m.Add(report), Times.Once);
            _context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Test]
        public void AddReport_DbUpdateException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new DbUpdateException());

            Assert.Throws<DbUpdateException>(() => _reportRepository.AddReport(new Report()));
        }

        [Test]
        public void AddReport_SqlException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(SqlExceptionCreator.Create());

            Assert.Throws<SqlException>(() => _reportRepository.AddReport(new Report()));
        }

        [Test]
        public void AddReport_EntityException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new EntityException());

            Assert.Throws<EntityException>(() => _reportRepository.AddReport(new Report()));
        }

        [Test]
        public void AddReport_TimeoutException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new TimeoutException());

            Assert.Throws<TimeoutException>(() => _reportRepository.AddReport(new Report()));
        }

        [Test]
        public void AddReport_GeneralException_RethrowsException()
        {
            _context.Setup(c => c.SaveChanges()).Throws(new Exception());

            Assert.Throws<Exception>(() => _reportRepository.AddReport(new Report()));
        }

        [Test]
        public void CountUniqueReports_ReportsExist_ReturnsCorrectUniqueCount()
        {
            Guid targetUser = Guid.NewGuid();
            Guid reporterA = Guid.NewGuid();
            Guid reporterB = Guid.NewGuid();

            _reportsData.Add(new Report { reporterUserID = reporterA, reportedUserID = targetUser });
            _reportsData.Add(new Report { reporterUserID = reporterA, reportedUserID = targetUser });
            _reportsData.Add(new Report { reporterUserID = reporterB, reportedUserID = targetUser });

            int count = _reportRepository.CountUniqueReports(targetUser);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void CountUniqueReports_NoReports_ReturnsZero()
        {
            Guid targetUser = Guid.NewGuid();

            int count = _reportRepository.CountUniqueReports(targetUser);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_SqlException_ReturnsZero()
        {
            _context.Setup(c => c.Reports).Throws(SqlExceptionCreator.Create());

            int count = _reportRepository.CountUniqueReports(Guid.NewGuid());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_EntityException_ReturnsZero()
        {
            _context.Setup(c => c.Reports).Throws(new EntityException());

            int count = _reportRepository.CountUniqueReports(Guid.NewGuid());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_TimeoutException_ReturnsZero()
        {
            _context.Setup(c => c.Reports).Throws(new TimeoutException());

            int count = _reportRepository.CountUniqueReports(Guid.NewGuid());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountUniqueReports_GeneralException_ReturnsZero()
        {
            _context.Setup(c => c.Reports).Throws(new Exception());

            int count = _reportRepository.CountUniqueReports(Guid.NewGuid());

            Assert.That(count, Is.EqualTo(0));
        }
    }
}