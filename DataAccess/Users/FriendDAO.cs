using DataAccess.DataRequests;
using DataAccess.Properties.Langs;
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
    public class FriendDAO : IFriendDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public FriendDAO() : this (new DbContextFactory()) { }

        public FriendDAO(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Searches for players matching a query string, excluding a specific player ID.
        /// </summary>
        public IEnumerable<Player> SearchPlayers(string query, Guid excludePlayerId, int limit = 20)
        {
            query = (query ?? "").Trim();
            if (string.IsNullOrEmpty(query)) return Enumerable.Empty<Player>();

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var q = query.ToLower();

                    return context.Players
                        .Include(p => p.User)
                        .Where(p => p.playerID != excludePlayerId &&
                                    (p.username.ToLower().Contains(q) ||
                                     (p.name != null && p.name.ToLower().Contains(q)) ||
                                     (p.lastName != null && p.lastName.ToLower().Contains(q))))
                        .OrderBy(p => p.username)
                        .Take(limit)
                        .AsNoTracking()
                        .ToList();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Error searching players with query: " + query, ex);
                return Enumerable.Empty<Player>();
            }
        }

        /// <summary>
        /// Retrieves the list of friends for a given player.
        /// </summary>
        public IEnumerable<Player> GetFriends(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return Enumerable.Empty<Player>();
            }

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

                    return context.Players
                             .Include(p => p.User)
                             .Where(p => ids.Contains(p.playerID))
                             .AsNoTracking()
                             .ToList();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Error retrieving friends for playerID: " + playerId, ex);
                return Enumerable.Empty<Player>();
            }
        }

        /// <summary>
        /// Retrieves pending friend requests for a specific player.
        /// </summary>
        public IEnumerable<Player> GetIncomingRequests(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return Enumerable.Empty<Player>();
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var requesterIds = context.Friendships
                        .Where(f => f.friendID == playerId && f.requestStatus == false)
                        .Select(f => f.playerID);

                    return context.Players
                             .Include(p => p.User)
                             .Where(p => requesterIds.Contains(p.playerID))
                             .AsNoTracking()
                             .ToList();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Error retrieving incoming requests for playerID: " + playerId, ex);
                return Enumerable.Empty<Player>();
            }
        }

        /// <summary>
        /// Sends a friend request from one player to another.
        /// </summary>
        public OperationResult SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var result = new OperationResult { Success = false };

            if (fromPlayerId == Guid.Empty || toPlayerId == Guid.Empty)
            {
                result.ErrorType = ErrorType.MISSING_DATA;
                result.Message = Lang.errorInvalidID;
                return result;
            }

            if (fromPlayerId == toPlayerId)
            {
                result.ErrorType = ErrorType.INVALID_DATA;
                result.Message = Lang.errorAddingYourself;
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
                        result.Message = Lang.errorDuplicatedFriendship;
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
                    result.Message = Lang.friendRequestSubmitted;
                    return result;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error($"Error sending friend request from {fromPlayerId} to {toPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                result.Message = Lang.errorUnprocessedRequest;
                return result;
            }
        }

        /// <summary>
        /// Accepts a pending friend request.
        /// </summary>
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
                                             f.requestStatus == false);

                    if (req == null)
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        result.Message = Lang.errorRequestNotFound;
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
                    result.Message = Lang.friendRequestAccepted;
                    return result;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error($"Error accepting friend request. User: {playerId}, Requester: {requesterPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                result.Message = Lang.errorAcceptingRequest;
                return result;
            }
        }

        /// <summary>
        /// Rejects a pending friend request.
        /// </summary>
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
                                             f.requestStatus == false);

                    if (req == null)
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        result.Message = Lang.errorRequestNotFound;
                        return result;
                    }

                    context.Friendships.Remove(req);
                    context.SaveChanges();

                    result.Success = true;
                    result.Message = Lang.friendRequestRejected;
                    return result;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error($"Error rejecting friend request. User: {playerId}, Requester: {requesterPlayerId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                result.Message = Lang.errorRejectingRequest;
                return result;
            }
        }

        /// <summary>
        /// Removes an existing friendship between two players.
        /// </summary>
        public OperationResult RemoveFriend(Guid playerId, Guid friendId)
        {
            var result = new OperationResult { Success = false };

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var links = context.Friendships.Where(f =>
                        (f.playerID == playerId && f.friendID == friendId && f.requestStatus == true) ||
                        (f.playerID == friendId && f.friendID == playerId && f.requestStatus == true)).ToList();

                    if (!links.Any())
                    {
                        result.ErrorType = ErrorType.NOT_FOUND;
                        result.Message = Lang.errorNoFriendship;
                        return result;
                    }

                    context.Friendships.RemoveRange(links);
                    context.SaveChanges();

                    result.Success = true;
                    result.Message = Lang.friendDeletedFriend;
                    return result;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error($"Error removing friend. User: {playerId}, Friend: {friendId}", ex);
                result.ErrorType = ErrorType.DB_ERROR;
                result.Message = Lang.errorDeletingFriend;
                return result;
            }
        }
    }
}