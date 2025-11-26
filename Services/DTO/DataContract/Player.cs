using System;
using System.Runtime.Serialization;

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

        public static DataAccess.Player AssembleDbPlayer(Player svPlayer)
        {
            DataAccess.Player dbPlayer = new DataAccess.Player();
            DataAccess.User dbUser = new DataAccess.User();
            dbPlayer.User = dbUser;

            if (!svPlayer.PlayerID.HasValue)
            {
                svPlayer.PlayerID = Guid.Empty;
            }

            dbPlayer.User.userID = svPlayer.User.UserID != Guid.Empty ? svPlayer.User.UserID : Guid.NewGuid();
            dbPlayer.User.email = svPlayer.User.Email;

            dbPlayer.playerID = (Guid)svPlayer.PlayerID != Guid.Empty ? (Guid)svPlayer.PlayerID : Guid.NewGuid();
            dbPlayer.username = svPlayer.Username;
            dbPlayer.avatarID = (byte?) svPlayer.AvatarID;
            dbPlayer.name = svPlayer.Name;
            dbPlayer.lastName = svPlayer.LastName;
            dbPlayer.facebookUsername = svPlayer.FacebookUsername;
            dbPlayer.instagramUsername = svPlayer.InstagramUsername;
            dbPlayer.discordUsername = svPlayer.DiscordUsername;

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

        public override bool Equals(object obj)
        {
            if (obj is Player other)
            {
                return PlayerID == other.PlayerID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return PlayerID.GetHashCode();
        }
    }
}
