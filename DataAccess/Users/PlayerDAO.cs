using DataAccess.DataRequests;
using DataAccess.Util;
using DataAccess.Validators;
using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Users
{
    public class PlayerDAO : IPlayerDAO
    {
        private readonly IDbContextFactory _contextFactory;
        public PlayerDAO() : this (new DbContextFactory()) { }

        public PlayerDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Player GetPlayerByUserID(Guid userID)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var query = from p in context.Players.Include(p => p.User)
                                where p.userID == userID
                                select p;
                    return query.FirstOrDefault();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("An exception ocurred while trying get a player by userID: ", ex);
                return null;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while getting a player by userID", ex);
                return null;
            }
        }

        public string GetEmailByPlayerID(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return string.Empty;
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    Player player = context.Players
                        .Include(p => p.User)
                        .AsNoTracking()
                        .FirstOrDefault(p => p.playerID == playerId);
                    return player != null ? player.User.email : string.Empty;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Failed to get a player's email: ", ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception trying to get a player's email: ", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if the email is already in use.
        /// </summary>
        /// <param name="username">The email to verify.</param>
        /// <returns>True if no matching email was found; false otherwise.</returns>
        /// </exception>
        public bool ValidateEmailNotDuplicated(string email)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    bool emailInUse = context.Users.Any(u => u.email == email);
                    return !emailInUse;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while verifying email not duplicated", ex);
                return false; //Assume it's in use if validation failed
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while verifying email not duplicated: ", ex);
                return false;
            }
            
        }

        /// <summary>
        /// Checks if the username is already in use.
        /// </summary>
        /// <param name="username">The username to verify.</param>
        /// <returns>True if no matching username was found, otherwise returns false.</returns>
        public bool ValidateUsernameNotDuplicated(string username)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    bool usernameInUse = context.Players.Any(p => p.username == username);
                    return !usernameInUse;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while verifying username not duplicated", ex);
                return false; //Assume it's in use if validation failed
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while verifying username not duplicated: ", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a Player entity by its ID, including its associated User.
        /// </summary>
        /// <param name="playerId">The ID of the player to search for.</param>
        /// <returns>The Player entity, or null if not found.</returns>
        public Player GetPlayerById(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return null;
            }
            try
            {
                using (var context = _contextFactory.Create())
                {
                    return context.Players
                             .Include(p => p.User)
                             .AsNoTracking()
                             .FirstOrDefault(p => p.playerID == playerId);
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while trying to get a Player by playerID: ", ex);
                return null;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while trying to get a Player by playerID: ", ex);
                return null;
            }
        }

        public bool VerifyIsPlayerGuest(Guid playerID)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var query = from p in context.Players
                                where p.playerID == playerID
                                select p;
                    return !query.Any();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Failed to verify if player is a guest: ", ex);
                return true; //We assume the player is a guest if its identity could not be verified
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while trying to verify if a player is a guest", ex);
                return true;
            }
        }

        public OperationResult UpdateProfile(Player updatedPlayer)
        {
            OperationResult result = new OperationResult();
            try
            {
                if (!PlayerValidator.ValidatePlayerProfile(updatedPlayer))
                {
                    result.Success = false;
                    result.ErrorType = ErrorType.INVALID_DATA;
                    return result;
                }

                using (var context = _contextFactory.Create())
                {
                    var dbUser = (from u in context.Users
                                  where u.userID == updatedPlayer.User.userID
                                  select u).FirstOrDefault();

                    if (dbUser == null)
                    {
                        result.Success = false;
                        result.ErrorType = ErrorType.NOT_FOUND;
                        return result;
                    }

                    result = HandleUserUpdate(updatedPlayer, dbUser);
                    if (!result.Success)
                    {
                        return result;
                    }

                    var dbPlayer = (from p in context.Players
                                    where p.playerID == updatedPlayer.playerID
                                    select p).FirstOrDefault();

                    if (dbPlayer == null)
                    {
                        result.Success = false;
                        result.ErrorType = ErrorType.NOT_FOUND;
                        return result;
                    }

                    result = HandlePlayerUpdate(updatedPlayer, dbPlayer);
                    if (result.Success)
                    {
                        UpdatePlayer(dbPlayer, updatedPlayer);
                        context.SaveChanges();
                    }
                    return result;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while trying to update an player's profile: ", ex);
                result.Success = false;
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while trying to update a player's profile: ", ex);
                result.Success = false;
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
        }

        private OperationResult HandleUserUpdate(Player updatedPlayer, User dbUser)
        {
            OperationResult result = new OperationResult();
            bool isEmailValidationNeeded = (updatedPlayer.User.email != dbUser.email);

            if (isEmailValidationNeeded)
            {
                if (!UserValidator.ValidateEmailFormat(updatedPlayer.User.email))
                {
                    result.Success = false;
                    result.ErrorType = ErrorType.INVALID_DATA;
                    return result;
                }

                if (ValidateEmailNotDuplicated(updatedPlayer.User.email))
                {
                    dbUser.email = updatedPlayer.User.email;
                }
                else
                {
                    result.Success = false;
                    result.ErrorType = ErrorType.DUPLICATE;
                    return result;
                }
            }
            result.Success = true;
            return result;
        }

        private OperationResult HandlePlayerUpdate(Player updatedPlayer, Player dbPlayer)
        {
            OperationResult result = new OperationResult();
            bool isUsernameValidationNeeded = (updatedPlayer.username != dbPlayer.username);
            if (isUsernameValidationNeeded)
            {
                if (ValidateUsernameNotDuplicated(updatedPlayer.username))
                {
                    result.Success = true;
                }
                else
                {
                    result.Success = false;
                    result.ErrorType = ErrorType.DUPLICATE;
                }
            }
            else
            {
                result.Success = true;
            }
            return result;
        }

        private static void UpdatePlayer(Player player, Player updatedPlayer)
        {
            player.username = updatedPlayer.username;
            player.avatarID = updatedPlayer.avatarID;
            player.name = updatedPlayer.name;
            player.lastName = updatedPlayer.lastName;
            player.facebookUsername = updatedPlayer.facebookUsername;
            player.instagramUsername = updatedPlayer.instagramUsername;
            player.discordUsername = updatedPlayer.discordUsername;
        }
    }
}
