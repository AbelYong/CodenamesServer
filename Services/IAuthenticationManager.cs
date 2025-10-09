using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Services
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        [OperationContract]
        bool Login(User user);

        // TODO: agregue aquí sus operaciones de servicio
    }

    [DataContract]
    public class User
    {
        private System.Guid userID;
        private string email;
        private string password;

        [DataMember]
        public System.Guid UserID {  get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Password { get; set; }
    }
}
