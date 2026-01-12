using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services
{
    [ServiceContract(CallbackContract = typeof(IFriendCallback))]
    public interface IFriendManager
    {
        [OperationContract]
        void Connect(Guid mePlayerId);

        [OperationContract]
        void Disconnect(Guid mePlayerId);

        [OperationContract]
        FriendshipRequest SendFriendRequest(Guid fromPlayerId, Guid toPlayerId);

        [OperationContract]
        FriendshipRequest AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        [OperationContract]
        FriendshipRequest RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        [OperationContract]
        FriendshipRequest RemoveFriend(Guid mePlayerId, Guid friendPlayerId);

        [OperationContract]
        FriendListRequest SearchPlayers(string query, Guid mePlayerId, int limit);

        [OperationContract]
        FriendListRequest GetFriends(Guid mePlayerId);

        [OperationContract]
        FriendListRequest GetIncomingRequests(Guid mePlayerId);

        [OperationContract]
        FriendListRequest GetSentRequests(Guid mePlayerId);
    }
}