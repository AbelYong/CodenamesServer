using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Contracts.Callback;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Works as a presence service, registers the beginning and end of game sessions,
    /// updates player's online/offline status and notifies the player's friends
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ISessionCallback))]
    public interface ISessionManager
    {
        /// <summary>
        /// Register's the client's callback channel, adds them to online players list,
        /// notifies the player's online friends upon success and sends the client's a list of their online friends.
        /// should be called immediately after login.
        /// </summary>
        /// <param name="player">A Player with a non-NULL playerID</param>
        /// <returns> A CommunicationRequest, IsSucess == True if the connection was successful;
        /// false otherwise along one of the following StatusCode:
        /// <para>MISSING_DATA if <paramref name="player"/> or <paramref name="player"/>.playerID is null </para>
        /// <para>UNAUTHORIZED if the login is duplicated, and the attempt to disconnect the online client failed</para>
        /// </returns>
        /// 
        [OperationContract]
        CommunicationRequest Connect(Player player);

        /// <summary>
        /// Removes the client's callback channel, removes them from the online players list and
        /// notifies the disconnection to the player's online friends. Should be called upon logout
        /// or client closing
        /// </summary>
        /// <param name="player">A Player with a non-NULL playerID</param>
        [OperationContract(IsOneWay = true)]
        void Disconnect(Player player);

        /// <summary>
        /// Allows the client to appear as online after adding a new friend.
        /// Should be called upon the acceptance of a friendship request
        /// </summary>
        /// <param name="friendA"></param>
        /// <param name="friendB"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyNewFriendship(Player friendA, Player friendB);

        /// <summary>
        /// Makes the player to appear as offline after removing a friend 
        /// Should be called upon a frienship's termination
        /// </summary>
        /// <param name="friendA"></param>
        /// <param name="friendB"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyFriendshipEnded(Player friendA, Player friendB);

        Player GetOnlinePlayer(Guid playerID);
        bool IsPlayerOnline(Guid playerId);
        void KickPlayer(Guid playerID, KickReason reason);
    }
}
