using DataAccess.DataRequests;
using DataAccess.Properties.Langs;
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
        public PlayerDAO() : this (new DbContextFactory())
        {

        }

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
            catch (SqlException sqlex)
            {
                DataAccessLogger.Log.Error("An error ocurred while trying get an user by ID: ", sqlex);
                return null;
            }
        }

        public string GetEmailByPlayerID(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return null;
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    Player player = context.Players.Include(p => p.User)
                        .AsNoTracking()
                        .FirstOrDefault(p => p.playerID == playerId);
                    return player != null ? player.User.email : string.Empty;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Failed to get a player's email: ", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if the email is already in use.
        /// </summary>
        /// <param name="username">The email to verify.</param>
        /// <returns>True if no matching email was found; false otherwise.</returns>
        /// <exception cref="System.Data.SqlClient.SqlException">
        /// <exception cref="EntityException">
        /// Thrown if the database operation failed.
        /// </exception>
        public bool ValidateEmailNotDuplicated(string email)
        {
            using (var context = _contextFactory.Create())
            {
                bool emailInUse = context.Users.Any(u => u.email == email);
                return !emailInUse;
            }
        }

        /// <summary>
        /// Checks if the username is already in use.
        /// </summary>
        /// <param name="username">The username to verify.</param>
        /// <returns>True if no matching username was found, otherwise returns false.</returns>
        /// <exception cref="System.Data.Entity.Core.EntityException">
        /// Thrown if the database operation failed.
        /// </exception>
        public bool ValidateUsernameNotDuplicated(string username)
        {
            using (var context = _contextFactory.Create())
            {
                bool usernameInUse = context.Players.Any(p => p.username == username);
                return !usernameInUse;
            }
        }

        /// <summary>
        /// Gets a Player entity by its ID, including its associated User.
        /// </summary>
        /// <param name="playerId">The ID of the player to search for.</param>
        /// <returns>The Player entity, or null if not found.</returns>
        public Player GetPlayerById(Guid playerId)
        {
            if (playerId == Guid.Empty) return null;

            using (var context = _contextFactory.Create())
            {
                return context.Players
                         .Include(p => p.User)
                         .AsNoTracking()
                         .FirstOrDefault(p => p.playerID == playerId);
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
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                //We assume the player is a guest if its identity could not be verified
                DataAccessLogger.Log.Error("Failed to verify if player is a guest: ", ex);
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
                DataAccessLogger.Log.Error("An error ocurred while trying to update an player's profile: ", ex);
                result.Success = false;
                result.Message = Lang.profileUpdateServerSideIssue;
                return result;
            }
        }

        private OperationResult HandleUserUpdate(Player updatedPlayer, User dbUser)
        {
            OperationResult result = new OperationResult();
            bool isEmailValidationNeeded = (updatedPlayer.User.email != dbUser.email);

            if (isEmailValidationNeeded)
            {
                if (ValidateEmailNotDuplicated(updatedPlayer.User.email))
                {
                    dbUser.email = updatedPlayer.User.email;
                }
                else
                {
                    result.Success = false;
                    result.Message = Lang.errorEmailAddressInUse;
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
                    result.Message = Lang.profileUpdateSuccess;
                }
                else
                {
                    result.Success = false;
                    result.Message = Lang.errorUsernameInUse;
                    result.ErrorType = ErrorType.DUPLICATE;
                }
            }
            else
            {
                result.Success = true;
                result.Message = Lang.profileUpdateSuccess;
            }
            return result;
        }

        public void DeletePlayer(Player playerToDelete)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    Player player = (from p in context.Players
                                     where p.username == playerToDelete.username
                                     select p).FirstOrDefault();
                    if (player != null)
                    {
                        Guid userID = player.userID;
                        User user = (from u in context.Users
                                     where u.userID == userID
                                     select u).FirstOrDefault();
                        context.Players.Remove(player);
                        context.Users.Remove(user);
                        context.SaveChanges();
                    }
                }
            }
            catch (SqlException sqlex)
            {
                System.Console.WriteLine(sqlex.Message);
            }
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
