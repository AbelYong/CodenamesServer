using System;
using System.Linq;
using System.Data.Entity;

namespace DataAccess.Users
{
    public class BanDAO : IBanDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public BanDAO() : this (new DbContextFactory()) { }

        public BanDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Ban GetActiveBan(Guid userID)
        {
            using (var context = _contextFactory.Create())
            {
                return context.Bans
                    .Where(b => b.userID == userID && b.timeout > DateTimeOffset.Now)
                    .OrderByDescending(b => b.timeout)
                    .FirstOrDefault();
            }
        }

        public void ApplyBan(Ban ban)
        {
            using (var context = _contextFactory.Create())
            {
                context.Bans.Add(ban);
                context.SaveChanges();
            }
        }
    }
}