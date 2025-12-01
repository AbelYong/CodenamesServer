using Services.DTO;
using Services.DTO.Request;
using Services.DTO.DataContract;
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
        List<Player> SearchPlayers(string query, Guid mePlayerId, int limit);

        [OperationContract]
        List<Player> GetFriends(Guid mePlayerId);

        [OperationContract]
        List<Player> GetIncomingRequests(Guid mePlayerId);

        [OperationContract]
        List<Player> GetSentRequests(Guid mePlayerId);
    }
}