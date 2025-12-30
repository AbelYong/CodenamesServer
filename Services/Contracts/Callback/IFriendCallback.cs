using Services.DTO.DataContract;
using System.ServiceModel;

namespace Services
{
    /// <summary>
    /// Callback contract that the *Client* must implement.
    /// Defines the notifications that the Server can send to the Client.
    /// </summary>
    [ServiceContract]
    public interface IFriendCallback
    {
        /// <summary>
        /// Notifies the client that it has received a new friend request.
        /// </summary>
        /// <param name="fromPlayer">The player sending the request.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyNewFriendRequest(Player fromPlayer);

        /// <summary>
        /// Notifies the client that their friend request has been accepted.
        /// </summary>
        /// <param name="byPlayer">The player who accepted the request.
        [OperationContract(IsOneWay = true)]
        void NotifyFriendRequestAccepted(Player byPlayer);

        /// <summary>
        /// Notifies the client that their friend request was rejected.
        /// </summary>
        /// <param name="byPlayer">The player who rejected the request.
        [OperationContract(IsOneWay = true)]
        void NotifyFriendRequestRejected(Player byPlayer);

        /// <summary>
        /// Notifies the client that a friend has removed them.
        /// </summary>
        /// <param name="byPlayer">The player who removed them.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyFriendRemoved(Player byPlayer);

        /// <summary>
        /// Notifies the client (requester) that their operation was successful.
        /// </summary>
        /// <param name="message">Confirmation message.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyOperationSuccess(string message);

        /// <summary>
        /// Notifies the client (requester) that their operation failed.
        /// This is for *business logic errors* (e.g., “User does not exist”),
        /// not for connection errors.
        /// </summary>
        /// <param name="message">Descriptive message about the error.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyOperationFailure(string message);
    }
}