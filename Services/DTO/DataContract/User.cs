using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
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
