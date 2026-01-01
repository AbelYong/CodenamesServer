using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used to represent the player's user, may be contained inside the associated User's Player
    /// </summary>
    [DataContract]
    public class User
    {
        [DataMember]
        public Guid UserID { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Password { get; set; }

        public static User AssembleSvUser(DataAccess.User dbUser)
        {
            User user = new User();
            user.UserID = dbUser.userID;
            user.Email = dbUser.email;
            user.Password = "";
            return user;
        }
    }
}
