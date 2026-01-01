using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    /// <summary>
    /// Notifies suscriptors of MatchmakingService events
    /// </summary>
    [ServiceContract]
    public interface IMatchmakingCallback
    {
        /// <summary>
        /// Called after a Match has been requested, means both clients are online and avaiable.
        /// The match is being generated and will be sent to the players after is ready
        /// </summary>
        /// <param name="requesterID"></param>
        /// <param name="companionID"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyRequestPending(Guid requesterID, Guid companionID);

        /// <summary>
        /// Sends a previously requested Match to the client
        /// </summary>
        /// <param name="match"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchReady(Match match);

        /// <summary>
        /// Notifies the client that both players have confirmed reception and are ready to play.
        /// </summary>
        /// <param name="matchID"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyPlayersReady(Guid matchID);

        /// <summary>
        /// Notifies the client that their companion has requested to cancel the match after receiving it
        /// </summary>
        /// <param name="matchID"></param>
        /// <param name="reason"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyMatchCanceled(Guid matchID, StatusCode reason);
    }
}
