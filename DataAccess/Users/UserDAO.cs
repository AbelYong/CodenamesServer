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
                // Create a StringBuilder to hold all the error messages
                var errorMessage = new System.Text.StringBuilder();

                Exception currentEx = sqlex;
                int level = 0;
                while (currentEx != null)
                {
                    errorMessage.AppendLine($"--- Level {level} ---");
                    errorMessage.AppendLine($"Type: {currentEx.GetType().FullName}");
                    errorMessage.AppendLine($"Message: {currentEx.Message}");
                    errorMessage.AppendLine(currentEx.StackTrace);
                    errorMessage.AppendLine();

                    currentEx = currentEx.InnerException;
                    level++;
                }

                // Print the full exception detail to the console
                System.Console.WriteLine(errorMessage.ToString());

                return null;
            }
            catch (InvalidCastException icex)
            {
                System.Console.WriteLine(icex.Message);
                //TODO log
                return null;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                //TODO log
                return null;
            }
        }
    }
}
