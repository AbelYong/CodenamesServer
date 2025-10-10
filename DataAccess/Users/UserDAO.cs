using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Entity.Core.Objects;

namespace DataAccess.Users
{
    public class UserDAO : IUserDAO
    {
        public Guid? Login(string username, string password)
        {
            try
            {
                Guid? userID = null;
                using (var context = new codenamesEntities())
                {
                    ObjectParameter outUserID = new ObjectParameter("userID", typeof(Guid?));
                    context.uspLogin(username, password, outUserID);
                    userID = (Guid?) outUserID.Value;
                }
                return userID;
            }
            catch (SqlException sqlex)
            {
                //TODO log
                return null;
            }
            catch (InvalidCastException icex)
            {
                //TODO log
                return null;
            }
            catch (Exception ex)
            {
                //TODO log
                return null;
            }
        }
    }
}
