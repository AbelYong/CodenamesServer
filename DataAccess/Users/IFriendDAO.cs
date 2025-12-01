using System;
using System.Collections.Generic;
using DataAccess.Util;

namespace DataAccess.Users
{
    public interface IFriendDAO
    {
        IEnumerable<Player> SearchPlayers(string query, Guid excludePlayerId, int limit = 20);
        IEnumerable<Player> GetFriends(Guid playerId);
        IEnumerable<Player> GetIncomingRequests(Guid playerId);
        IEnumerable<Player> GetSentRequests(Guid playerId);
        OperationResult SendFriendRequest(Guid fromPlayerId, Guid toPlayerId);
        OperationResult AcceptFriendRequest(Guid playerId, Guid requesterPlayerId);
        OperationResult RejectFriendRequest(Guid playerId, Guid requesterPlayerId);
        OperationResult RemoveFriend(Guid playerId, Guid friendId);
    }
}