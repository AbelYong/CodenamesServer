using DataAccess.DataRequests;
using DataAccess.Util;
using DataAccess.Validators;
using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IPlayerRepository _playerRepository;

        public UserRepository() : this (new DbContextFactory(), new PlayerRepository()) { }

        public UserRepository(IDbContextFactory contextFactory, IPlayerRepository playerRepository)
        {
            _contextFactory = contextFactory;
            _playerRepository = playerRepository;
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
            PlayerRegistrationRequest request = new PlayerRegistrationRequest();
            try
            {
                request = ValidateNewUser(player, password);
                if (!request.IsSuccess)
                {
                    return request;
                }
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
            request.IsSuccess = true;
            if (player == null || player.User == null)
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.MISSING_DATA;
                return request;
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

            if (!_playerRepository.ValidateEmailNotDuplicated(player.User.email))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DUPLICATE;
                request.IsEmailDuplicate = true;
            }

            if (!_playerRepository.ValidateUsernameNotDuplicated(player.username))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DUPLICATE;
                request.IsUsernameDuplicate = true;
            }

            if (!PlayerValidator.ValidatePlayerProfile(player))
            {
                request.IsPasswordValid = false;
                request.ErrorType = ErrorType.INVALID_DATA;
            }
            return request;
        }

        public UpdateRequest ResetPassword(string email, string newPassword)
        {
            UpdateRequest request = new UpdateRequest();
            if (!UserValidator.ValidatePassword(newPassword))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.INVALID_DATA;
                return request;
            }

            if (!UserValidator.ValidatePassword(newPassword))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.INVALID_DATA;
                return request;
            }

            try
            {
                if (CheckEmailExists(email))
                {
                    using (var context = _contextFactory.Create())
                    {
                        context.uspUpdatePassword(email, newPassword);
                        request.IsSuccess = true;
                    }
                }
                else
                {
                    request.IsSuccess= false;
                    request.ErrorType = ErrorType.NOT_FOUND;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while trying to reset an user's password: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while trying to reset an user's password: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
            }
            return request;
        }

        public UpdateRequest UpdatePassword(string username, string currentPassword, string newPassword)
        {
            UpdateRequest request = new UpdateRequest();
            if (!UserValidator.ValidatePassword(newPassword))
            {
                request.IsSuccess = false;
                request.ErrorType = ErrorType.INVALID_DATA;
                return request;
            }

            try
            {
                string email = GetEmailByUsername(username);
                if (string.IsNullOrEmpty(email))
                {
                    request.IsSuccess = false;
                    request.ErrorType = ErrorType.NOT_FOUND;
                    return request;
                }

                if (InternalAuthenticate(username, currentPassword))
                {
                    using (var context = _contextFactory.Create())
                    {
                        context.uspUpdatePassword(email, newPassword);
                        request.IsSuccess = true;
                    }
                }
                else
                {
                    request.IsSuccess = false;
                    request.ErrorType = ErrorType.UNALLOWED;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while trying to update an user's password: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while trying to update an user's password: ", ex);
                request.IsSuccess = false;
                request.ErrorType = ErrorType.DB_ERROR;
            }
            return request;
        }

        private bool CheckEmailExists(string email)
        {
            using (var context = _contextFactory.Create())
            {
                return context.Users.Where(u => u.email == email).Any();
            }
        }

        private string GetEmailByUsername(string username)
        {
            string email = string.Empty;
            using (var context = _contextFactory.Create())
            {
                Player auxPlayer = context.Players
                    .Include(p => p.User)
                    .Where(p => p.username == username).FirstOrDefault();
                if (auxPlayer != null)
                {
                    email = auxPlayer.User.email;
                }
            }
            return email;
        }

        private bool InternalAuthenticate(string username, string password)
        {
            Guid? userID = Authenticate(username, password);
            if (userID != null)
            {
                return true;
            }
            return false;
        }
    }
}
