using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    [DataContract]
    public class Player
    {
        [DataMember]
        private Guid? PlayerID { get; set; }

        [DataMember]
        private string Username {  get; set; }

        [DataMember]
        private int AvatarID { get; set; }

        [DataMember]
        private string Name { get; set; }

        [DataMember]
        private string LastName { get; set; }

        [DataMember]
        private string FacebookUsername { get; set; }

        [DataMember]
        private string InstagramUsername { get; set; }

        [DataMember]
        private string DiscordUsername { get; set; }

        [DataMember]
        private User User { get; set; }

        public static DataAccess.Player AssembleDbPlayer(User svUser, Player svPlayer)
        {
            DataAccess.User dbUser = new DataAccess.User();
            dbUser.email = svUser.Email;

            DataAccess.Player dbPlayer = new DataAccess.Player();
            dbPlayer.username = svPlayer.Username;
            dbPlayer.name = svPlayer.Name;
            dbPlayer.lastName = svPlayer.LastName;
            dbPlayer.User = dbUser;

            return dbPlayer;
        }

        public static Player AssembleSvPlayer(DataAccess.Player dbPlayer)
        {
            if (dbPlayer != null)
            {
                Player svPlayer = new Player();
                svPlayer.PlayerID = dbPlayer.playerID;
                svPlayer.Username = dbPlayer.username;
                svPlayer.AvatarID = dbPlayer.avatarID.GetValueOrDefault();
                svPlayer.Name = dbPlayer.name;
                svPlayer.LastName = dbPlayer.lastName;
                svPlayer.FacebookUsername = dbPlayer.facebookUsername;
                svPlayer.InstagramUsername = dbPlayer.instagramUsername;
                svPlayer.DiscordUsername = dbPlayer.discordUsername;
                svPlayer.User = User.AssembleSvUser(dbPlayer.User);
                return svPlayer;
            }
            else
            {
                return null;
            }
        }
    }
}
