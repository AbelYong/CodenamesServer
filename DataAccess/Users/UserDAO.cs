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
        public Guid? Authenticate(string username, string password)
        {
            try
            {
                Guid? userID = null;
                using (var context = new codenamesEntities())
                {
                    ObjectParameter outUserID = new ObjectParameter("userID", typeof(Guid?));
                    context.uspLogin(username, password, outUserID);
                    var databaseValue = outUserID.Value;
                    userID = (databaseValue != DBNull.Value) ? (Guid?) databaseValue : null;
                }
                return userID;
            }
            catch (SqlException sqlex)
            {
                //TODO log
                System.Console.WriteLine("SqlException");
                System.Console.WriteLine(sqlex.Message);
                throw;
            }
        }

        public Guid? SignIn(Player player, string password)
        {
            try
            {
                Guid? newUserID = null;
                using (var context = new codenamesEntities())
                {
                    ObjectParameter outUserID = new ObjectParameter("newUserID", typeof(Guid?));
                    context.uspSignIn(player.User.email, password, player.username, player.name, player.lastName, outUserID);
                    var databaseValue = outUserID.Value;
                    newUserID = (databaseValue != DBNull.Value) ? (Guid?) databaseValue : null;
                }
                return newUserID;
            }
            catch (SqlException sqlex)
            {
                //TODO log
                System.Console.WriteLine(sqlex.Message);
                return null;
            }
            catch (Exception ex)
            {
                //TODO log
                System.Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks if the email is already in use.
        /// </summary>
        /// <param name="username">The email to verify.</param>
        /// <returns>True if no matching email was found; false otherwise.</returns>
        /// <exception cref="System.Data.SqlClient.SqlException">
        /// Thrown if the database operation failed.
        /// </exception>
        public static bool ValidateEmailNotDuplicated(string email)
        {
            using (var context = new codenamesEntities())
            {
                bool emailInUse = context.Users.Any(u => u.email == email);
                return !emailInUse;
            }
        }
    }
}
