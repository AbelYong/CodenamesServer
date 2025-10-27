using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    [DataContract]
    public class User
    {
        [DataMember]
        public System.Guid UserID { get; set; }

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
