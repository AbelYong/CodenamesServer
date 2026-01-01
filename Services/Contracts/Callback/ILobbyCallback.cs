using Services.DTO.DataContract;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    /// <summary>
    /// Notifies Lobby Events to the clients
    /// </summary>
    [ServiceContract]
    public interface ILobbyCallback
    {
        /// <summary>
        /// Called when a player sends an ingame invitation to a player who's currently online
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="lobbyCode"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationReceived(Player fromPlayer, string lobbyCode);

        /// <summary>
        /// Notifies the PartyHost that someone has accepted their invitation and sends them the PartyGuest Profile
        /// </summary>
        /// <param name="byPlayer"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationAccepted(Player byPlayer);

        /// <summary>
        /// Notifies either the PartyHost or the PartyGuest that their companion has left the Party
        /// </summary>
        /// <param name="leavingPlayerID"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyPartyAbandoned(Guid leavingPlayerID);
    }
}
