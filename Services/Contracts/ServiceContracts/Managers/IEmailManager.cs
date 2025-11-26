using Services.DTO;
using Services.DTO.Request;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IEmailManager
    {
        [OperationContract]
        CommunicationRequest SendVerificationCode(string email);

        [OperationContract]
        ConfirmEmailRequest ValidateVerificationCode(string email, string code);
    }
}
