using Services.DTO;
using Services.DTO.Request;
using System.ServiceModel;

namespace Services.Contracts
{
    [ServiceContract(CallbackContract = typeof(ISessionCallback))]
    public interface ISessionManager
    {
        [OperationContract]
        CommunicationRequest Connect(Player player);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Player player);
    }
}
