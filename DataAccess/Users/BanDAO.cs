using DataAccess.Util;
using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Users
{
    public class BanDAO : IBanDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public BanDAO() : this(new DbContextFactory()) { }

        public BanDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Ban GetActiveBan(Guid userID)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    return context.Bans
                        .Where(b => b.userID == userID && b.timeout > DateTimeOffset.Now)
                        .OrderByDescending(b => b.timeout)
                        .FirstOrDefault();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Exception while retrieving active ban for user: ", ex);
                return null;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while retrieving active ban for user: ", ex);
                return null;
            }
        }

        public void ApplyBan(Ban ban)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    context.Bans.Add(ban);
                    context.SaveChanges();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Exception while applying ban: ", ex);
                throw;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while applying ban: ", ex);
                throw;
            }
        }
    }
}