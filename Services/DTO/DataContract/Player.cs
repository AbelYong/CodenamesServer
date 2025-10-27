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
        public Guid? PlayerID { get; set; }

        [DataMember]
        public string Username {  get; set; }

        [DataMember]
        public int AvatarID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string FacebookUsername { get; set; }

        [DataMember]
        public string InstagramUsername { get; set; }

        [DataMember]
        public string DiscordUsername { get; set; }

        [DataMember]
        public User User { get; set; }

        public static DataAccess.Player AssembleDbPlayer(User svUser, Player svPlayer)
        {
            DataAccess.User dbUser = new DataAccess.User();
            dbUser.userID = svUser.UserID;
            dbUser.email = svUser.Email;

            DataAccess.Player dbPlayer = new DataAccess.Player();
            dbPlayer.playerID = (Guid) svPlayer.PlayerID;
            dbPlayer.username = svPlayer.Username;
            dbPlayer.avatarID = (byte?) svPlayer.AvatarID;
            dbPlayer.name = svPlayer.Name;
            dbPlayer.lastName = svPlayer.LastName;
            dbPlayer.facebookUsername = svPlayer.FacebookUsername;
            dbPlayer.instagramUsername = svPlayer.InstagramUsername;
            dbPlayer.discordUsername = svPlayer.DiscordUsername;
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
