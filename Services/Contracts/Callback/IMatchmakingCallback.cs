using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    [ServiceContract]
    public interface IMatchmakingCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyRequestPending(Guid requesterID, Guid companionID);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchReady(Match match);

        [OperationContract(IsOneWay = true)]
        void NotifyPlayersReady(Guid matchID);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchCanceled(Guid matchID, StatusCode reason);
    }
}
