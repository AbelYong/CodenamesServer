using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Services.DTO;

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
    }

    [DataContract]
    public class ResetResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
    }
}
