using System.Runtime.Serialization;
using System.ServiceModel;
using Services.DTO;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        [OperationContract]
        LoginRequest Login(string username, string password);

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
