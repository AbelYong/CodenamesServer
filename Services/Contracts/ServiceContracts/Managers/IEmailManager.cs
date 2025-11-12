using Services.DTO;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IEmailManager
    {
        [OperationContract]
        RequestResult SendVerificationCode(string email);

        [OperationContract]
        RequestResult ValidateVerificationCode(string email, string code);
    }
}
