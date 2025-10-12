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
        Guid? Login(string username, string password);

        [OperationContract]
        Guid? SignIn(User svUser, Player svPlayer);

        [OperationContract]
        void BeginPasswordReset(string username, string email);

        [OperationContract]
        ResetResult CompletePasswordReset(string username, string code, string newPassword);

        [OperationContract]
        BeginRegistrationResult BeginRegistration(User svUser, Player svPlayer, string plainPassword);

        [OperationContract]
        ConfirmRegistrationResult ConfirmRegistration(Guid requestId, string code);

        [OperationContract]
        void CancelRegistration(Guid requestId);
    }

    [DataContract]
    public class BeginRegistrationResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public Guid? RequestId { get; set; }
    }

    [DataContract]
    public class ConfirmRegistrationResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public Guid? UserId { get; set; }
    }

    [DataContract]
    public class ResetResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class User
    {
        private System.Guid _userID;
        private string _email;
        private string _password;

        [DataMember]
        public System.Guid UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        [DataMember]
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        [DataMember]
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }
    }

    [DataContract]
    public class Player
    {
        private string _username;
        private string _name;
        private string _lastName;

        [DataMember]
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }
        [DataMember]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        [DataMember]
        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }
    }
}
