using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Allows users to create Parties, Invite player's to their party, Send Invitations to their party,
    /// Join another player's party, aswell as leaving their own or another player's party
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ILobbyCallback), SessionMode = SessionMode.Required)]
    public interface ILobbyManager
    {
        /// <summary>
        /// Register's the client's callback channel and allows them to use the rest of LobbyService's operations
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns>A CommunicationRequest IsSuccess == True, otherwise IsSuccess == False
        /// along the StatusCode UNAUTHORIZED if the client couldn't be registered to the service
        /// </returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        /// <summary>
        /// Removes the client's callback channel and removes them from the Party,
        /// also removes their Party if they're the Host of their current Party
        /// </summary>
        /// <param name="playerID"></param>
        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        /// <summary>
        /// Allows the client to request a Party of which they will become Host.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>A CreateLobbyRequest containing the Party's LobyCode and IsSuccess == True if the Lobby could be created,
        /// otherwise IsSucess == False along one of the following StatusCode:
        /// <para>MISSING_DATA if player or their playerID is NULL</para>
        /// <para>SERVER_ERROR if the party couldn't be created (This occurs if the generated code conflicts with another party, client should retry)</para>
        /// <para>UNALLOWED if the player is already in a Party</para>
        /// </returns>
        [OperationContract(IsOneWay = false)]
        CreateLobbyRequest CreateParty(Player player);

        /// <summary>
        /// Sends an invitation to friend's email, along an in-game invitation if the selected friend is online
        /// </summary>
        /// <param name="partyHost">The player's profile, including their playerID</param>
        /// <param name="friendToInviteID">The playerID of the player to invite</param>
        /// <param name="lobbyCode">The lobby code provided by the server when the Party was created</param>
        /// <returns>A CommunicationRequest IsSuccess == True if the invitation was succesfully sent.
        /// Otherwise IsSucess == False along one of the following StatusCode
        /// <para>MISSING_DATA if partyHost or partyHost.playerID is NULL</para>
        /// <para>CLIENT_UNREACHABLE if the email couldn't be sent to the requested friend</para>
        /// </returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest InviteToParty(Player partyHost, Guid friendToInviteID, string lobbyCode);

        /// <summary>
        /// Allows the client to join another player's existing Party through the Party's LobbyCode
        /// </summary>
        /// <param name="joiningPlayer">The client's player profile</param>
        /// <param name="lobbyCode">A six-digit alphanumeric code matching an existing Party's LobbyCode</param>
        /// <returns>A JoinPartyRequest IsSuccess == True if the registration to Party was sucessful,
        /// otherwise IsSucess == False along one of the following StatusCode:
        /// <para>CLIENT_DISCONNECT if joiningPlayer's playerID doesn't match one of a registered client</para>
        /// <para>NOT_FOUND if the provided lobbyCode doesn't match the code of an existing Party</para>
        /// <para>CONFLICT if the requested Party the client is trying to join is already full</para>
        /// <para>CLIENT_UNREACHABLE if the Party was found and has an avaiable guest slot, but the PartyHost couldn't be notified</para>
        /// </returns>
        [OperationContract(IsOneWay = false)]
        JoinPartyRequest JoinParty(Player joiningPlayer, string lobbyCode);

        /// <summary>
        /// Allows the client to leave their current Party, also removes the Party if the client is their current Party's host
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="lobbyCode"></param>
        [OperationContract(IsOneWay = true)]
        void LeaveParty(Guid playerID, string lobbyCode);
    }
}
