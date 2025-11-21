using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(IMatchCallback), SessionMode = SessionMode.Required)]
    public interface IMatchManager
    {
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        [OperationContract(IsOneWay = false)]
        CommunicationRequest JoinMatch(Match match, Guid playerID);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        [OperationContract(IsOneWay = true)]
        void SendClue(Guid senderID, string clue);

        [OperationContract(IsOneWay = true)]
        void NotifyTurnTimeout(Guid senderID, MatchRoleType currentRole);

        [OperationContract(IsOneWay = true)]
        void NotifyPickedAgent(Guid senderID, int newTurnLength);

        [OperationContract(IsOneWay = true)]
        void NotifyPickedBystander(Guid senderID, TokenType tokenToUpdate);

        [OperationContract(IsOneWay = true)]
        void NotifyPickedAssassin(Guid senderID);
    }
}
