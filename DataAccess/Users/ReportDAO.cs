using DataAccess.Util;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Users
{
    public class ReportDAO : IReportDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public ReportDAO() : this(new DbContextFactory()) { }

        public ReportDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public bool HasPlayerReportedTarget(Guid reporterUserID, Guid reportedUserID)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    return context.Reports.Any(r =>
                        r.reporterUserID == reporterUserID &&
                        r.reportedUserID == reportedUserID);
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Exception while checking if player has reported target: ", ex);
                return false;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while checking if player has reported target: ", ex);
                return false;
            }
        }

        public void AddReport(Report report)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    context.Reports.Add(report);
                    context.SaveChanges();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Exception while adding a new report: ", ex);
                throw;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while adding a new report: ", ex);
                throw;
            }
        }

        public int CountUniqueReports(Guid reportedUserID)
        {
            try
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
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Exception while counting unique reports: ", ex);
                return 0;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while counting unique reports: ", ex);
                return 0;
            }
        }
    }
}