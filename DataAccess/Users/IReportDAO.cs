using System;

namespace DataAccess.Users
{
    public interface IReportDAO
    {
        bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID);
        void AddReport(Report report);
        int CountUniqueReports(Guid reportedUserID);
    }
}