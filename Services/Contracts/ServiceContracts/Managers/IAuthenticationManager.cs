using System.Runtime.Serialization;
using System.ServiceModel;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        [OperationContract]
        AuthenticationRequest Authenticate(string username, string password);

        [OperationContract]
        CommunicationRequest CompletePasswordReset(string email, string code, string newPassword);

        [OperationContract]
        CommunicationRequest UpdatePassword(string username, string currentPassword, string newPassword);
    }
}
