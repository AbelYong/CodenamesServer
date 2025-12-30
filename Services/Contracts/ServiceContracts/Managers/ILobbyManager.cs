using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(ILobbyCallback), SessionMode = SessionMode.Required)]
    public interface ILobbyManager
    {
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        [OperationContract(IsOneWay = false)]
        CreateLobbyRequest CreateParty(Player player);

        [OperationContract(IsOneWay = false)]
        CommunicationRequest InviteToParty(Player partyHost, Guid friendToInviteID, string lobbyCode);

        [OperationContract(IsOneWay = false)]
        JoinPartyRequest JoinParty(Player joiningPlayer, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void LeaveParty(Guid playerID, string lobbyCode);
    }
}
