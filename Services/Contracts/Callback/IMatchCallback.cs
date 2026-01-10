using Services.DTO.DataContract;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    /// <summary>
    /// Notifies MatchService suscriptors of gameplay events
    /// </summary>
    [ServiceContract]
    public interface IMatchCallback
    {
        /// <summary>
        /// Pushes chat messages on suscriptors
        /// </summary>
        /// <param name="clue"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyClueReceived(string clue);

        /// <summary>
        /// Notfies the players that the Spymaster has run out of time to type a clue
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyTurnChange();

        /// <summary>
        /// Notifies the players that the Guesser's turn has ended and roles have been switched on the server
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyRolesChanged();

        /// <summary>
        /// Notifies the Spymaster that the Guesser has run out of time. (But roles have not yet changed on the server)
        /// </summary>
        /// <param name="timerTokens">The amount of remaining tokens after the timeout,
        /// to which the Spymaster should update their TimerTokens</param>
        [OperationContract(IsOneWay = true)]
        void NotifyGuesserTurnTimeout(int timerTokens);

        /// <summary>
        /// Notifies the Spymaster that the Guesser has picked an agent
        /// </summary>
        /// <param name="notification">An AgentPickedNotification including the BoardCoordinates with the Row and Column
        /// for the Agent picked, along with the NewTurnLength, to which the Spymaster must sychronize their own timer</param>
        [OperationContract(IsOneWay = true)]
        void NotifyAgentPicked(AgentPickedNotification notification);

        /// <summary>
        /// Notifies the Spymaster that the Guesser has picked a bystander
        /// </summary>
        /// <param name="notification">A BystanderPickedNotification including the BoardCoordinates with the Row and Column
        /// for the Bystander picked,both the TokenToUpdate and RemainingTokens are calculated by the server</param>
        [OperationContract(IsOneWay = true)]
        void NotifyBystanderPicked(BystanderPickedNotification notification);

        /// <summary>
        /// Notifies the spymaster that the Guesser has picked an assassin
        /// </summary>
        /// <param name="notification">An AssassinPickedNotification including the BoardCoordinates with the Row and Column
        /// for the Assassin picked, the Match's lenght at game over is calculated by the Server-side timer</param>
        [OperationContract(IsOneWay = true)]
        void NotifyAssassinPicked(AssassinPickedNotification notification);

        /// <summary>
        /// Notifies the players that they have found all agents and Match has ended in victory
        /// </summary>
        /// <param name="finalMatchLength">The Match's lenght at game over. (According to the Server-side timer)</param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchWon(string finalMatchLength);

        /// <summary>
        /// Notifies the players that match ended due to either Timeout on the Guesser's last turn, or TimerToken exhaustion
        /// </summary>
        /// <param name="finalMatchLength">The Match's lenght at game over. (According to the Server-side timer)</param>
        /// <param name="isTimeOut">If True, means the Match ended because the Guesser ran out of time in their turn
        /// without meeting any other of the victory/defeat conditions. Otherwise, means the Match ended because it
        /// has ran out of TimerTokens</param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchTimeout(string finalMatchLength, bool isTimeOut);

        /// <summary>
        /// Notifies the players that their current companion has disconnected (callback channel faulted),
        /// and the Match has been cancelled
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyCompanionDisconnect();

        /// <summary>
        /// Notifies the players that despite the match ending succesfully, through either Victory or Assassin defeat,
        /// the player's Scoreboard couldn't be updated on the database
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyStatsCouldNotBeSaved();

        [OperationContract(IsOneWay = true)]
        void CheckPlayerStatus();
    }
}
