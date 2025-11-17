using System;
using System.Linq;

namespace DataAccess.Users
{
    public class ReportDAO : IReportDAO
    {
        public bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID)
        {
            using (var context = new codenamesEntities())
            {
                return context.Reports.Any(r =>
                    r.reporterUserID == reporterUserID &&
                    r.reportedUserID == reportedUserID);
            }
        }

        public void AddReport(Report report)
        {
            using (var context = new codenamesEntities())
            {
                context.Reports.Add(report);
                context.SaveChanges();
            }
        }

        public int CountUniqueReports(Guid reportedUserID)
        {
            using (var context = new codenamesEntities())
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