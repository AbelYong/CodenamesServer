using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using DataAccess.Properties.Langs;
using DataAccess.Util;

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

        public OperationResult UpdateProfile(Player updatedPlayer)
        {
            OperationResult result = new OperationResult();
            try
            {
                using (var context = new codenamesEntities())
                {
                    var dbUser = (from u in context.Users
                                where u.userID == updatedPlayer.userID
                                select u).FirstOrDefault();
                    dbUser.email = updatedPlayer.User.email;

                    var dbPlayer = (from p in context.Players
                                  where p.playerID == updatedPlayer.playerID
                                  select p).FirstOrDefault();
                    UpdatePlayer(dbPlayer, updatedPlayer);
                    context.SaveChanges();

                    result.Success = true;
                    result.Message = Lang.profileUpdateSuccess;
                    return result;
                }
            }
            catch (SqlException sqlex)
            {
                //TODO log
                result.Success = false;
                result.Message = Lang.profileUpdateServerSideIssue;
                return result;
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
