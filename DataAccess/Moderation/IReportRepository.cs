using System;

namespace DataAccess.Moderation
{
    public interface IReportRepository
    {
        bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID);
        void AddReport(Report report);
        int CountUniqueReports(Guid reportedUserID);
    }
}