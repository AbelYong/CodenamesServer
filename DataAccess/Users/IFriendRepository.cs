using System;
using System.Collections.Generic;
using DataAccess.Util;
using DataAccess.DataRequests;

namespace DataAccess.Users
{
    public interface IFriendRepository
    {
        PlayerListRequest SearchPlayers(string query, Guid excludePlayerId, int limit = 20);
        PlayerListRequest GetFriends(Guid playerId);
        PlayerListRequest GetIncomingRequests(Guid playerId);
        PlayerListRequest GetSentRequests(Guid playerId);
        OperationResult SendFriendRequest(Guid fromPlayerId, Guid toPlayerId);
        OperationResult AcceptFriendRequest(Guid playerId, Guid requesterPlayerId);
        OperationResult RejectFriendRequest(Guid playerId, Guid requesterPlayerId);
        OperationResult RemoveFriend(Guid playerId, Guid friendId);
    }
}