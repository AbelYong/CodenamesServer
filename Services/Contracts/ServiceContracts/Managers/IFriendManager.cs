using Services.DTO;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services
{
    /// <summary>
    /// Main contract for managing friends and requests in real time.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IFriendCallback))]
    public interface IFriendManager
    {
        /// <summary>
        /// Connects the client to the friends service and registers its callback channel.
        /// </summary>
        /// <param name="mePlayerId">The ID of the player connecting.</param>
        [OperationContract(IsOneWay = true)]
        void Connect(Guid mePlayerId);

        /// <summary>
        /// Disconnects the client and clears its callback channel.
        /// </summary>
        /// <param name="mePlayerId">The ID of the player being disconnected.</param>
        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid mePlayerId);

        /// <summary>
        /// Sends a friend request to another player.
        /// The result (success/error) is notified to both players via callback.
        /// </summary>
        /// <param name="fromPlayerId">The ID of the sending player. </param>
        /// <param name="toPlayerId">The ID of the receiving player. </param>
        [OperationContract(IsOneWay = true)]
        void SendFriendRequest(Guid fromPlayerId, Guid toPlayerId);

        /// <summary>
        /// Accepts a friend request.
        /// The result is notified to both players via callback.
        /// </summary>
        /// <param name="mePlayerId">The ID of the player accepting. </param>
        /// <param name="requesterPlayerId">The ID of the player who sent the request. </param>
        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        /// <summary>
        /// Rejects a friend request.
        /// The result is notified to both players via callback.
        /// </summary>
        /// <param name="mePlayerId">The ID of the player who is rejecting the request. </param>
        /// <param name="requesterPlayerId">The ID of the player who sent the request. </param>
        [OperationContract(IsOneWay = true)]
        void RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        /// <summary>
        /// Deletes a friend.
        /// The result is notified to both players via callback.
        /// </summary>
        /// <param name="mePlayerId">The ID of the player doing the deleting. </param>
        /// <param name="friendPlayerId">The ID of the friend to be deleted. </param>
        [OperationContract(IsOneWay = true)]
        void RemoveFriend(Guid mePlayerId, Guid friendPlayerId);

        [OperationContract]
        List<Player> SearchPlayers(string query, Guid mePlayerId, int limit);

        [OperationContract]
        List<Player> GetFriends(Guid mePlayerId);

        [OperationContract]
        List<Player> GetIncomingRequests(Guid mePlayerId);
    }
}