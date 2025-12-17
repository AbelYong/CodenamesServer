using Services.DTO.DataContract;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    /// <summary>
    /// Callback interface to notify clients of updates to the scoreboard.
    /// </summary>
    [ServiceContract]
    public interface IScoreboardCallback
    {
        /// <summary>
        /// Notifies clients with the updated list of the best players.
        /// </summary>
        /// <param name="leaderboard">The updated list of scores.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyLeaderboardUpdate(List<Scoreboard> leaderboard);
    }
}