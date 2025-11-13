using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(IMatchmakingCallback))]
    public interface IMatchmakingManager
    {
        [OperationContract]
        MatchRequest GetMatchWithAFriend(MatchConfiguration configuration);

        [OperationContract]
        CommunicationRequest CancelMatch();
    }
}
