using Services.DTO;
using System.ServiceModel;

namespace Services.Contracts
{
    [ServiceContract(CallbackContract = typeof(ISessionCallback))]
    public interface ISessionManager
    {
        [OperationContract]
        void Connect(Player player);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Player player);
    }
}
