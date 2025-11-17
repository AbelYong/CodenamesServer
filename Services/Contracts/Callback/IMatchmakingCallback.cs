using Services.DTO;
using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

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
