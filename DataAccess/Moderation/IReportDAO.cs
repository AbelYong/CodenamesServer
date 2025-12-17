using System;

namespace DataAccess.Moderation
{
    public interface IReportDAO
    {
        bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID);
        void AddReport(Report report);
        int CountUniqueReports(Guid reportedUserID);
    }
}