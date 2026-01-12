using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
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
        ScoreboardRequest GetMyScore(Guid playerID);

        [OperationContract]
        ScoreboardRequest GetTopPlayers();
        void NotifyMatchConcluded();
    }
}