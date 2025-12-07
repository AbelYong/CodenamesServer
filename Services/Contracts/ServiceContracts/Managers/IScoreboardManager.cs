using Services.Contracts.Callback;
using Services.DTO.DataContract;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(IScoreboardCallback))]
    public interface IScoreboardManager
    {
        [OperationContract]
        void SubscribeToScoreboardUpdates(Guid playerID);

        [OperationContract]
        void UnsubscribeFromScoreboardUpdates(Guid playerID);

        [OperationContract]
        Scoreboard GetMyScore(Guid playerID);
    }
}