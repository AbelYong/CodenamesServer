using DataAccess.DataRequests;
using DataAccess.Properties.Langs;
using DataAccess.Util;
using DataAccess.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

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
                DataAccessLogger.Log.Error("An error ocurred while trying get an user by ID: ", sqlex);
                return null;
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

            using (var db = new codenamesEntities())
            {
                return db.Players
                         .Include(p => p.User)
                         .AsNoTracking()
                         .FirstOrDefault(p => p.playerID == playerId);
            }
        }

        public static string GetEmailByPlayerID(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return null;
            }

            try
            {
                using (var db = new codenamesEntities())
                {
                    Player player = db.Players.Include(p => p.User)
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

        public static bool VerifyIsPlayerGuest(Guid playerID)
        {
            try
            {
                using (var context = new codenamesEntities())
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

                using (var context = new codenamesEntities())
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

        private static OperationResult HandleUserUpdate(Player updatedPlayer, User dbUser)
        {
            OperationResult result = new OperationResult();
            bool isEmailValidationNeeded = (updatedPlayer.User.email != dbUser.email);

            if (isEmailValidationNeeded)
            {
                if (UserDAO.ValidateEmailNotDuplicated(updatedPlayer.User.email))
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

        private static OperationResult HandlePlayerUpdate(Player updatedPlayer, Player dbPlayer)
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

        public static void DeletePlayer(Player playerToDelete)
        {
            try
            {
                using (var context = new codenamesEntities())
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

        /// <summary>
        /// Checks if the username is already in use.
        /// </summary>
        /// <param name="username">The username to verify.</param>
        /// <returns>True if no matching username was found, otherwise returns false.</returns>
        /// <exception cref="System.Data.Entity.Core.EntityException">
        /// Thrown if the database operation failed.
        /// </exception>
        public static bool ValidateUsernameNotDuplicated(string username)
        {
            using (var context = new codenamesEntities())
            {
                bool usernameInUse = context.Players.Any(p => p.username == username);
                return !usernameInUse;
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
