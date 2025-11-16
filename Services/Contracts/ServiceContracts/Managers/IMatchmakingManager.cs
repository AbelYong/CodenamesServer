using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(IMatchmakingCallback), SessionMode = SessionMode.Required)]
    public interface IMatchmakingManager
    {
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        [OperationContract(IsOneWay = false)]
        CommunicationRequest RequestArrangedMatch(MatchConfiguration configuration);

        [OperationContract(IsOneWay = true)]
        void ConfirmMatchReceived(Guid playerID, Guid matchID);

        [OperationContract(IsOneWay = true)]
        void RequestMatchCancel(Guid playerID);
    }
}
