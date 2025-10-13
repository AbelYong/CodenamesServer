using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace DataAccess.Users
{
    public class PlayerDAO : IPlayerDAO
    {
        public Player GetPlayerByUserID(Guid userID)
        {
            try
            {
                using (var context = new codenamesEntities())
                {
                    var query = from p in context.Players.Include(p => p.User)
                                where p.userID == userID
                                select p;
                    return query.FirstOrDefault();
                }
            }
            catch (SqlException sqlex)
            {
                //Todo log
                return null;
            }
        }
    }
}
