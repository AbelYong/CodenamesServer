using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Manages ongoing matches and handles active gameplay actions by the players
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IMatchCallback), SessionMode = SessionMode.Required)]
    public interface IMatchManager
    {
        /// <summary>
        /// Register's the clients callback channel, which allows them to consume the rest of MatchmakingService's operations
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns>A CommunicationRequest IsSuccess == True if the connection was accepted.
        /// Otherwise IsSuccess == False along the StatusCode Unauthorized if the connection was rejected. (Already connected and failed to reconnect)</returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        /// <summary>
        /// Allows player's to join a match independently of if they're the Match's requester or the companion
        /// </summary>
        /// <param name="match">The match sent to the player's by MatchmakingService</param>
        /// <param name="playerID">The client's playerID, it must match either the Match's Requester or the Companion's playerID</param>
        /// <returns>A CommunicationRequest IsSuccess = True if the client has been succesfully asigned to an ongoing match.
        /// Otherwise IsSuccess == False along one of the following StatusCode:
        /// <para>MISSING_DATA if Match is null</para>
        /// <para>WRONG_DATA if the provided playerID doesn't match either the Match's Requester or Companion playerID</para>
        /// <para>UNALLOWED if the player cannot join a match, due to already being registered for an ongoing match</para>
        /// <para>SERVER_ERROR if the Match couldn't be registered (This may only occur due to a Guid collision, client should request a new Match)</para>
        /// </returns>
        [OperationContract(IsOneWay = false)]
        CommunicationRequest JoinMatch(Match match, Guid playerID);

        /// <summary>
        /// Unregister's a players callback channel, and cancels any ongoing matches they were in
        /// </summary>
        /// <param name="playerID"></param>
        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        /// <summary>
        /// Sends a clue to the ongoing match's guesser.
        /// </summary>
        /// <param name="senderID">The playerID of an existing ongoing match's current Spymaster</param>
        /// <param name="clue">The clue sent to the </param>
        [OperationContract(IsOneWay = false)]
        void SendClue(Guid senderID, string clue);

        /// <summary>
        /// Notifies the server that the player has ran out of time for their role in the current turn,
        /// either allows Guesser to choose cards or notifies players to change roles
        /// </summary>
        /// <param name="senderID">The playerID of an existing ongoing match</param>
        /// <param name="currentRole">Either SPYMASTER or GUESSER</param>
        [OperationContract(IsOneWay = false)]
        void NotifyTurnTimeout(Guid senderID, MatchRoleType currentRole);

        /// <summary>
        /// Notifies the server that an agent has been picked so that the current spymaster is informed,
        /// will trigger victory events if number of remaining agents to discover reaches zero.
        /// </summary>
        /// <param name="notification">Includes the senderID with must belong to a Gusser in an ongoing Match,
        /// Row and Column of BoardCoordinates must be a number between 0 and 4, the NewTurnLength must be a number
        /// inferior to 60></param>
        [OperationContract(IsOneWay = false)]
        void NotifyPickedAgent(AgentPickedNotification notification);

        /// <summary>
        /// Notifies the server that a bystander has been picked, triggers role change.
        /// Ends the match in defeat if Match ran out of TimerTokens after token substraction
        /// </summary>
        /// <param name="notification">Includes the senderID with must belong to a Guesser in an ongoing Match,
        /// Row and Column of BoardCoordinates must be a number between 0 and 4, TokenToUpdate (TIMER, BYSTANDER)
        /// must be specified. RemainingTokens is calculated by the server, its value will be ignored if set></param>
        [OperationContract(IsOneWay = false)]
        void NotifyPickedBystander(BystanderPickedNotification notification);

        /// <summary>
        /// Notifies the server that an assassin has been picked so that current spymaster is informed.
        /// Ends the match immediately in defeat
        /// </summary>
        /// <param name="notification">Includes the senderID with must belong to a Guesser in an ongoing Match,
        /// Row and Column of BoardCoordinates must be a number between 0 and 4, FinalMatchLength is calculated
        /// by the server, its value will be ignored if set</param>
        [OperationContract(IsOneWay = false)]
        void NotifyPickedAssassin(AssassinPickedNotification notification);

        [OperationContract(IsOneWay = false)]
        bool CheckCompanionStatus(Guid senderID);
    }
}
