using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Manages the creation of matches, allows player's to request matches, cancel them, 
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IMatchmakingCallback), SessionMode = SessionMode.Required)]
    public interface IMatchmakingManager
    {
        /// <summary>
        /// Register's the client's callback channel, allowing the use of the rest of MatchmakingService's operations
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns></returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        /// <summary>
        /// Unregister's the client's callback channel and removes any pending matches they are involved in
        /// </summary>
        /// <param name="playerID"></param>
        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        /// <summary>
        /// Creates a new Match given the provided MatchConfiguration
        /// </summary>
        /// <param name="configuration">
        /// A match configuration which must include both the Requester and their Companion's playerID,
        /// and MatchRules (with a mandatory Gamemode), for Gamemode.CUSTOM values for TurnTimer, TimerTokens and BystanderTokens are expected
        /// </param>
        /// <returns>A CommunicationRequest, IsSuccess == True if the match was generated and sent to both players,
        /// otherwise IsSuccess == False along one of the following StatusCode
        /// <para>MISSING_DATA if Requester, Companion or their respective playerID is NULL</para>
        /// <para>CONFLICT if a Match can't be arranged because either Requester or Companion are already in a Match</para>
        /// <para>CLIENT_UNREACHABLE if either the RequestReceived or MatchReceived notifications failed for any of the clients</para>
        /// <para>SERVER_ERROR means the request failed because the Match couldn't be registered as pending.
        /// (Duplicate MatchID, the ID is set by the server as GUID, if received client should retry)</para>
        /// </returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest RequestArrangedMatch(MatchConfiguration configuration);

        /// <summary>
        /// Allows the client to confirm they have received the match and are ready to play
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="matchID">The matchID of the Match sent to the client by the NotifyMatchReady callback method</param>
        [OperationContract(IsOneWay = true)]
        void ConfirmMatchReceived(Guid playerID, Guid matchID);

        /// <summary>
        /// Allows the client to cancel a match after it's been received. Has no effect if the player has no pending matches
        /// </summary>
        /// <param name="playerID"></param>
        [OperationContract(IsOneWay = true)]
        void RequestMatchCancel(Guid playerID);
    }
}
