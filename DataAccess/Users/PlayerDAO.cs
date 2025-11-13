using DataAccess.Properties.Langs;
using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
                //TODO log
                Console.WriteLine("An error ocurred while trying to authenticate an user: {0}", sqlex.Message);
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

        public OperationResult UpdateProfile(Player updatedPlayer)
        {
            OperationResult result = new OperationResult();
            try
            {
                using (var context = new codenamesEntities())
                {
                    var dbUser = (from u in context.Users
                                  where u.userID == updatedPlayer.User.userID
                                  select u).FirstOrDefault();

                    result = HandleUserUpdate(updatedPlayer, dbUser);
                    if (!result.Success)
                    {
                        return result;
                    }

                    var dbPlayer = (from p in context.Players
                                    where p.playerID == updatedPlayer.playerID
                                    select p).FirstOrDefault();

                    result = HandlePlayerUpdate(updatedPlayer, dbPlayer);
                    if (result.Success)
                    {
                        UpdatePlayer(dbPlayer, updatedPlayer);
                        context.SaveChanges();
                    }
                    return result;
                }
            }
            catch (SqlException sqlex)
            {
                //TODO log
                Console.WriteLine("An error ocurred while trying to update an user's profile: {0}", sqlex.Message);
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
                if (!ValidateUsernameNotDuplicated(updatedPlayer.username))
                {
                    result.Success = false;
                    result.Message = Lang.errorUsernameInUse;
                }
                else
                {
                    result.Success = true;
                    result.Message = Lang.profileUpdateSuccess;
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
                //TODO log
                System.Console.WriteLine(sqlex.Message);
            }
        }

        private static bool ValidatePlayerProfile(Player player)
        {
            if (player == null)
            {
                return false;
            }
            if (!ValidateIdentificationData(player))
            {
                return false;
            }
            return true;
        }

        private static bool ValidateIdentificationData(Player player)
        {
            const int MAX_USERNAME_LENGTH = 20;
            const int MAX_EMAIL_LENGTH = 30;
            if (string.IsNullOrEmpty(player.User.email) || player.User.email.Length > MAX_EMAIL_LENGTH)
            {
                return false;
            }
            if (string.IsNullOrEmpty(player.username) || player.username.Length > MAX_USERNAME_LENGTH)
            {
                return false;
            }
            return true;
        }

        private static bool ValidatePersonalData(Player player)
        {
            const int MAX_NAME_LENGTH = 20;
            const int MAX_LASTNAME_LENGTH = 30;
            if (player.name.Length > MAX_NAME_LENGTH)
            {
                return false;
            }
            if (player.lastName.Length > MAX_LASTNAME_LENGTH)
            {
                return false;
            }
            return true;
        }

        private static bool ValidateSocialMediaData(Player player)
        {
            const int SOCIAL_MEDIA_LENGTH = 30;
            if (player.facebookUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            if (player.instagramUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            if (player.discordUsername.Length > SOCIAL_MEDIA_LENGTH)
            {
                return false;
            }
            return true;
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
