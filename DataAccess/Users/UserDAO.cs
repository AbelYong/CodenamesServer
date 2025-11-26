using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Entity.Core.Objects;
using DataAccess.DataRequests;
using DataAccess.Validators;
using System.Data.Entity.Infrastructure;

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

        public PlayerRegistrationRequest SignIn(Player player, string password)
        {
            PlayerRegistrationRequest request = ValidateNewUser(player, password);
            if (!request.IsSuccess)
            {
                return request;
            }
            try
            {
                request.IsEmailValid = true;
                request.IsEmailDuplicate = false;
                request.IsUsernameDuplicate = false;
                request.IsPasswordValid = true;
                Guid? newUserID = null;
                using (var context = new codenamesEntities())
                {
                    ObjectParameter outUserID = new ObjectParameter("newUserID", typeof(Guid?));
                    context.uspSignIn(player.User.email, password, player.username, player.name, player.lastName, outUserID);
                    var databaseValue = outUserID.Value;
                    newUserID = (databaseValue != DBNull.Value) ? (Guid?) databaseValue : Guid.Empty;
                }
                request.IsSuccess = true;
                request.NewPlayerID = newUserID;
                return request;
            }
            catch (SqlException sqlex)
            {
                //TODO log
                System.Console.WriteLine(sqlex.Message);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
                return request;
            }
            catch (Exception ex)
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
                return request;
            }
        }

        private static PlayerRegistrationRequest ValidateNewUser(Player player, string password)
        {
            PlayerRegistrationRequest request = new PlayerRegistrationRequest();
            request.IsSuccess = true; //Start assuming it's valid
            if (player == null || player.User == null)
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.MISSING_DATA;
            }

            if (!ValidateEmailNotDuplicated(player.User.email))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DUPLICATE;
                request.IsEmailDuplicate = true;
            }

            if (!PlayerDAO.ValidateUsernameNotDuplicated(player.username))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DUPLICATE;
                request.IsUsernameDuplicate = true;
            }

            if (!UserValidator.ValidateEmailFormat(player.User.email))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.INVALID_DATA;
                request.IsEmailValid = false;
            }

            if (!UserValidator.ValidatePassword(password))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.INVALID_DATA;
                request.IsPasswordValid = false;
            }
            return request;
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
