using System;
using System.Linq;
using System.Data.SqlClient;
using System.Data.Entity.Core.Objects;
using DataAccess.DataRequests;
using DataAccess.Validators;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core;
using DataAccess.Util;

namespace DataAccess.Users
{
    public class UserDAO : IUserDAO
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IPlayerDAO _playerDAO;

        public UserDAO() : this (new DbContextFactory(), new PlayerDAO()) { }

        public UserDAO(IDbContextFactory contextFactory, IPlayerDAO playerDAO)
        {
            _contextFactory = contextFactory;
            _playerDAO = playerDAO;
        }

        /// <summary>
        /// Gets the User's userID if the provided username and password match 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The user's id if the username and password match, otherwise returns null</returns>
        /// 
        public Guid? Authenticate(string username, string password)
        {
            try
            {
                Guid? userID = null;
                using (var context = _contextFactory.Create())
                {
                    ObjectParameter outUserID = new ObjectParameter("userID", typeof(Guid?));
                    context.uspLogin(username, password, outUserID);
                    var databaseValue = outUserID.Value;
                    userID = (databaseValue != DBNull.Value) ? (Guid?) databaseValue : null;
                }
                return userID;
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
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
                SetRequestToValid(request);
                Guid? newUserID = null;
                using (var context = _contextFactory.Create())
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
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while signing-in a new player: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
                return request;
            }
            catch (Exception ex) when (ex is InvalidCastException)
            {
                DataAccessLogger.Log.Debug("Cast exception while trying to cast Guid from uspSignIn: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
                return request;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while signing in a new player: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
                return request;
            }
        }

        private static void SetRequestToValid(PlayerRegistrationRequest request)
        {
            request.IsEmailValid = true;
            request.IsEmailDuplicate = false;
            request.IsUsernameDuplicate = false;
            request.IsPasswordValid = true;
        }

        private PlayerRegistrationRequest ValidateNewUser(Player player, string password)
        {
            PlayerRegistrationRequest request = new PlayerRegistrationRequest();
            request.IsSuccess = true; //Start assuming it's valid
            if (player == null || player.User == null)
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.MISSING_DATA;
                return request; //Stop, following validations will throw NullReference
            }

            if (!_playerDAO.ValidateEmailNotDuplicated(player.User.email))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DUPLICATE;
                request.IsEmailDuplicate = true;
            }

            if (!_playerDAO.ValidateUsernameNotDuplicated(player.username))
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
    }
}
