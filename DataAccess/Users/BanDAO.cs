using System;
using System.Linq;
using System.Data.Entity;

namespace DataAccess.Users
{
    public class BanDAO : IBanDAO
    {
        public Ban GetActiveBan(Guid userID)
        {
            using (var context = new codenamesEntities())
            {
                return context.Bans
                    .Where(b => b.userID == userID && b.timeout > DateTimeOffset.Now)
                    .OrderByDescending(b => b.timeout)
                    .FirstOrDefault();
            }
        }

        public void ApplyBan(Ban ban)
        {
            using (var context = new codenamesEntities())
            {
                context.Bans.Add(ban);
                context.SaveChanges();
            }
        }
    }
}