using DataAccess.DataRequests;
using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Users
{
    public class FriendRepository : IFriendRepository
    {
        private readonly IDbContextFactory _contextFactory;

        public FriendRepository() : this(new DbContextFactory()) { }

        public FriendRepository(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public PlayerListRequest SearchPlayers(string query, Guid excludePlayerId, int limit = 20)
        {
            PlayerListRequest result = new PlayerListRequest();
            query = (query ?? "").Trim();
            if (string.IsNullOrEmpty(query))
            {
                result.IsSuccess = true;
                return result;
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var q = query.ToLower();
                    var existingContacts = context.Friendships
                        .Where(f => (f.playerID == excludePlayerId || f.friendID == excludePlayerId) && f.requestStatus)
                        .Select(f => f.playerID == excludePlayerId ? f.friendID : f.playerID)
                        .ToList();

                    var searchResult = context.Players.Include("User")
                        .Where(p => p.username.ToLower().Contains(q) &&
                                    p.playerID != excludePlayerId &&
                                    !existingContacts.Contains(p.playerID))
                        .Take(limit)
                        .ToList();

                    result.Players = searchResult;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException) 
            {
                DataAccessLogger.Log.Error("Error searching players: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
                result.Players = new List<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error searching players: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            return result;
        }

        public PlayerListRequest GetFriends(Guid playerId)
        {
            PlayerListRequest result = new PlayerListRequest();
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var friendsA = context.Friendships
                        .Where(f => f.playerID == playerId && f.requestStatus)
                        .Select(f => f.friendID);

                    var friendsB = context.Friendships
                        .Where(f => f.friendID == playerId && f.requestStatus)
                        .Select(f => f.playerID);

                    var ids = friendsA.Union(friendsB);

                    var friends = context.Players
                        .Include(p => p.User)
                        .Where(p => ids.Contains(p.playerID))
                        .AsNoTracking()
                        .ToList();

                    result.Players = friends;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                DataAccessLogger.Log.Error("Error retrieving friends: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving friends: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            return result;
        }

        public PlayerListRequest GetIncomingRequests(Guid playerId)
        {
            PlayerListRequest result = new PlayerListRequest();
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var requesterIds = context.Friendships
                        .Where(f => f.friendID == playerId && !f.requestStatus)
                        .Select(f => f.playerID);

                    var requests = context.Players
                             .Include(p => p.User)
                             .Where(p => requesterIds.Contains(p.playerID))
                             .AsNoTracking()
                             .ToList();

                    result.Players = requests;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                DataAccessLogger.Log.Error("Error retrieving incoming requests: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
                result.Players = new List<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving incoming requests: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            return result;
        }

        public PlayerListRequest GetSentRequests(Guid playerId)
        {
            PlayerListRequest result = new PlayerListRequest();
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var targetIds = context.Friendships
                        .Where(f => f.playerID == playerId && !f.requestStatus)
                        .Select(f => f.friendID);

                    var requests = context.Players
                        .Include(p => p.User)
                        .Where(p => targetIds.Contains(p.playerID))
                        .AsNoTracking()
                        .ToList();

                    result.Players = requests;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex) when (ex is SqlException || (ex is EntityException))
            {
                DataAccessLogger.Log.Error("Error retrieving sent requests: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
                result.Players = new List<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving sent requests: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            return result;
        }

        public OperationResult SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var result = new OperationResult { Success = false };

            if (fromPlayerId == Guid.Empty || toPlayerId == Guid.Empty)
            {
                result.ErrorType = ErrorType.MISSING_DATA;
                return result;
            }

            if (fromPlayerId == toPlayerId)
            {
                result.ErrorType = ErrorType.INVALID_DATA;
                return result;
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var alreadyExists = context.Friendships.Any(f =>
                        (f.playerID == fromPlayerId && f.friendID == toPlayerId) ||
                        (f.playerID == toPlayerId && f.friendID == fromPlayerId));

                    if (alreadyExists)
                    {
                        result.ErrorType = ErrorType.DUPLICATE;
                        return result;
                    }

                    context.Friendships.Add(new Friendship
                    {
                        playerID = fromPlayerId,
                        friendID = toPlayerId,
                        requestStatus = false
                    });

                    context.SaveChanges();

                    result.Success = true;
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                DataAccessLogger.Log.Debug($"Error saving friend request to DB from {fromPlayerId} to {toPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug($"Entity error sending friend request from {fromPlayerId} to {toPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Debug($"SQL error sending friend request from {fromPlayerId} to {toPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error($"Unexpected error sending friend request from {fromPlayerId} to {toPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
        }

        public OperationResult AcceptFriendRequest(Guid playerId, Guid requesterPlayerId)
        {
            var result = new OperationResult { Success = false };

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var req = context.Friendships
                        .FirstOrDefault(f => f.playerID == requesterPlayerId &&
                                             f.friendID == playerId &&
                                             !f.requestStatus);

                    if (req == null)
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        return result;
                    }

                    req.requestStatus = true;

                    var reciprocalExists = context.Friendships.Any(f =>
                        f.playerID == playerId && f.friendID == requesterPlayerId && f.requestStatus);

                    if (!reciprocalExists)
                    {
                        context.Friendships.Add(new Friendship
                        {
                            playerID = playerId,
                            friendID = requesterPlayerId,
                            requestStatus = true
                        });
                    }

                    context.SaveChanges();

                    result.Success = true;
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                DataAccessLogger.Log.Debug($"Error saving changes (Accept Request). User: {playerId}, Requester: {requesterPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug($"Entity error accepting friend request. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error($"Unexpected error accepting friend request. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
        }

        public OperationResult RejectFriendRequest(Guid playerId, Guid requesterPlayerId)
        {
            var result = new OperationResult { Success = false };

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var req = context.Friendships
                        .FirstOrDefault(f => f.playerID == requesterPlayerId &&
                                             f.friendID == playerId &&
                                             !f.requestStatus);

                    if (req == null)
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        return result;
                    }

                    context.Friendships.Remove(req);
                    context.SaveChanges();

                    result.Success = true;
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                DataAccessLogger.Log.Debug($"Error saving changes (Reject Request). User: {playerId}, Requester: {requesterPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug($"Entity error rejecting friend request. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error($"Unexpected error rejecting friend request. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
        }

        public OperationResult RemoveFriend(Guid playerId, Guid friendId)
        {
            var result = new OperationResult { Success = false };

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var links = context.Friendships.Where(f =>
                        (f.playerID == playerId && f.friendID == friendId && f.requestStatus) ||
                        (f.playerID == friendId && f.friendID == playerId && f.requestStatus)).ToList();

                    if (!links.Any())
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        return result;
                    }

                    context.Friendships.RemoveRange(links);
                    context.SaveChanges();

                    result.Success = true;
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                DataAccessLogger.Log.Debug($"Error saving changes (Remove Friend). User: {playerId}, Friend: {friendId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug($"Entity error removing friend. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error($"Unexpected error removing friend. User: {playerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                return result;
            }
        }
    }
}