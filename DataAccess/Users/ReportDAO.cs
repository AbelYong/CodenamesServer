using System;
using System.Linq;

namespace DataAccess.Users
{
    public class ReportDAO : IReportDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public ReportDAO() : this (new DbContextFactory()) { }

        public ReportDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID)
        {
            using (var context = _contextFactory.Create())
            {
                return context.Reports.Any(r =>
                    r.reporterUserID == reporterUserID &&
                    r.reportedUserID == reportedUserID);
            }
        }

        public void AddReport(Report report)
        {
            using (var context = _contextFactory.Create())
            {
                context.Reports.Add(report);
                context.SaveChanges();
            }
        }

        public int CountUniqueReports(Guid reportedUserID)
        {
            using (var context = _contextFactory.Create())
            {
                return context.Reports
                    .Where(r => r.reportedUserID == reportedUserID)
                    .Select(r => r.reporterUserID)
                    .Distinct()
                    .Count();
            }
        }
    }
}