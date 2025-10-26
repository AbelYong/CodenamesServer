using System;
using System.Runtime.Serialization;
using System.ServiceModel;
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
}
