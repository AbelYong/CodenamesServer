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
    public class FriendDAO : IFriendDAO
    {
        private readonly IDbContextFactory _contextFactory;

        public FriendDAO() : this(new DbContextFactory()) { }

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
            if (string.IsNullOrEmpty(query))
            {
                return Enumerable.Empty<Player>();
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var q = query.ToLower();
                    var existingContacts = context.Friendships
                        .Where(f => (f.playerID == excludePlayerId || f.friendID == excludePlayerId) && f.requestStatus)
                        .Select(f => f.playerID == excludePlayerId ? f.friendID : f.playerID);

                    return context.Players
                        .Include(p => p.User)
                        .Where(p => p.playerID != excludePlayerId &&
                                    !existingContacts.Contains(p.playerID) &&
                                    (p.username.ToLower().Contains(q) ||
                                     (p.name != null && p.name.ToLower().Contains(q)) ||
                                     (p.lastName != null && p.lastName.ToLower().Contains(q))))
                        .OrderBy(p => p.username)
                        .Take(limit)
                        .AsNoTracking()
                        .ToList();
                }
            }
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Error("Database connection error searching players.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Error("Entity error searching players.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Error("Timeout searching players.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error searching players.", ex);
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
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Debug("Database connection error retrieving friends.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug("Entity error retrieving friends.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (TimeoutException ex)
            {
                DataAccessLogger.Log.Debug("Timeout retrieving friends.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving friends.", ex);
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
                        .Where(f => f.friendID == playerId && !f.requestStatus)
                        .Select(f => f.playerID);

                    return context.Players
                             .Include(p => p.User)
                             .Where(p => requesterIds.Contains(p.playerID))
                             .AsNoTracking()
                             .ToList();
                }
            }
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Debug("Database connection error retrieving incoming requests.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug("Entity error retrieving incoming requests.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving incoming requests.", ex);
                return Enumerable.Empty<Player>();
            }
        }

        public IEnumerable<Player> GetSentRequests(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return Enumerable.Empty<Player>();
            }

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var targetIds = context.Friendships
                        .Where(f => f.playerID == playerId && !f.requestStatus)
                        .Select(f => f.friendID);

                    return context.Players
                        .Include(p => p.User)
                        .Where(p => targetIds.Contains(p.playerID))
                        .AsNoTracking()
                        .ToList();
                }
            }
            catch (SqlException ex)
            {
                DataAccessLogger.Log.Debug("Database connection error retrieving sent requests.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (EntityException ex)
            {
                DataAccessLogger.Log.Debug("Entity error retrieving sent requests.", ex);
                return Enumerable.Empty<Player>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected error retrieving sent requests.", ex);
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