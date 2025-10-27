using Services.DTO;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Services
{
    [ServiceContract]
    public interface IFriendManager
    {
        [OperationContract]
        List<Player> SearchPlayers(string query, Guid mePlayerId, int limit);

        [OperationContract]
        List<Player> GetFriends(Guid mePlayerId);

        [OperationContract]
        List<Player> GetIncomingRequests(Guid mePlayerId);

        [OperationContract]
        OperationResultSv SendFriendRequest(Guid fromPlayerId, Guid toPlayerId);

        [OperationContract]
        OperationResultSv AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        [OperationContract]
        OperationResultSv RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId);

        [OperationContract]
        OperationResultSv RemoveFriend(Guid mePlayerId, Guid friendPlayerId);
    }

    [DataContract]
    public class OperationResultSv
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
    }
}