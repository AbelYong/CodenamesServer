using Services.DTO;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    [ServiceContract]
    public interface ILobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationReceived(Player fromPlayer, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationAccepted(Player byPlayer);

        [OperationContract(IsOneWay = true)]
        void NotifyPartyAbandoned(Guid leavingPlayerID);
    }
}
