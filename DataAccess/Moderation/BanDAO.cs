using DataAccess.Util;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Moderation
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
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Error("Database connection error retrieving active ban for user: " + userID, ex);
                return null;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Error("Entity error retrieving active ban for user: " + userID, ex);
                return null;
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Error("Timeout retrieving active ban for user: " + userID, ex);
                return null;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while retrieving active ban for user: " + userID, ex);
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
            catch (DbUpdateException ex)
            {
                DataAccessLogger.Log.Error("Error updating database while applying ban.", ex);
                throw;
            }
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Error("SQL error applying ban.", ex);
                throw;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Error("Entity error applying ban.", ex);
                throw;
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Error("Timeout applying ban.", ex);
                throw;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while applying ban.", ex);
                throw;
            }
        }
    }
}