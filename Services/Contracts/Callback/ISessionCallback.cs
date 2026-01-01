using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    /// <summary>
    /// Notifies SessionService's suscriptors when their friends connect or disconnect.
    /// sends the initial list of online friends to the player, and updates the show online/offline status
    /// on friendship status modification
    /// </summary>
    [ServiceContract]
    public interface ISessionCallback
    {
        /// <summary>
        /// Called on all suscriptors who are currently friends with the parameter player's when they connect to SessionService
        /// </summary>
        /// <param name="player"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyFriendOnline(Player player);

        /// <summary>
        /// Called on all suscriptors who are currently friends with the player matching playerID when they disconnect from SessionService
        /// </summary>
        /// <param name="playerId"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyFriendOffline(Guid playerId);

        /// <summary>
        /// Called once a player succesfully connects to SessionService, sending them a list of their friends who are currently online
        /// </summary>
        /// <param name="friends"></param>
        [OperationContract(IsOneWay = true)]
        void ReceiveOnlineFriends(List<Player> friends);

        /// <summary>
        /// Called when a player logs-in but have already have an active session to end the previous session,
        /// called by moderation service when a player has accomulated enough reports to warrant an expulsion
        /// </summary>
        /// <param name="reason"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyKicked(KickReason reason);
    }
}
