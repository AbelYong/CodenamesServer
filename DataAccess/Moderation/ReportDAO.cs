using DataAccess.Util;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Moderation
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
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Error("Database connection error checking if player has reported target.", ex);
                return false;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Error("Entity error checking if player has reported target.", ex);
                return false;
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Error("Timeout checking if player has reported target.", ex);
                return false;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while checking if player has reported target.", ex);
                return false;
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
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Error("Database connection error counting unique reports.", ex);
                return 0;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Error("Entity error counting unique reports.", ex);
                return 0;
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Error("Timeout counting unique reports.", ex);
                return 0;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while counting unique reports.", ex);
                return 0;
            }
        }
    }
}