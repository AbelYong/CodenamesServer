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
        void ReceiveMatch(Match match);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchCanceled();
    }
}
