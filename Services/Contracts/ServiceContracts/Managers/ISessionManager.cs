using Services.DTO;
using Services.DTO.Request;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(ISessionCallback))]
    public interface ISessionManager
    {
        [OperationContract]
        CommunicationRequest Connect(Player player);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Player player);

        [OperationContract(IsOneWay = true)]
        void NotifyNewFriendship(Player friendA, Player friendB);

        [OperationContract(IsOneWay = true)]
        void NotifyFriendshipEnded(Player friendA, Player friendB);
    }
}
