using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DataAccess.Util;

namespace DataAccess.Users
{
    public class FriendDAO : IFriendDAO
    {
        public IEnumerable<Player> SearchPlayers(string query, Guid excludePlayerId, int limit = 20)
        {
            using (var db = new codenamesEntities())
            {
                query = (query ?? "").Trim();
                if (string.IsNullOrEmpty(query)) return Enumerable.Empty<Player>();

                var q = query.ToLower();

                return db.Players
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

        public IEnumerable<Player> GetFriends(Guid playerId)
        {
            using (var db = new codenamesEntities())
            {
                var friendsA = db.Friendships
                                 .Where(f => f.playerID == playerId && f.requestStatus == true)
                                 .Select(f => f.friendID);

                var friendsB = db.Friendships
                                 .Where(f => f.friendID == playerId && f.requestStatus == true)
                                 .Select(f => f.playerID);

                var ids = friendsA.Union(friendsB);

                return db.Players
                         .Include(p => p.User)
                         .Where(p => ids.Contains(p.playerID))
                         .AsNoTracking()
                         .ToList();
            }
        }

        public IEnumerable<Player> GetIncomingRequests(Guid playerId)
        {
            using (var db = new codenamesEntities())
            {
                var requesterIds = db.Friendships
                    .Where(f => f.friendID == playerId && f.requestStatus == false)
                    .Select(f => f.playerID);

                return db.Players
                         .Include(p => p.User)
                         .Where(p => requesterIds.Contains(p.playerID))
                         .AsNoTracking()
                         .ToList();
            }
        }

        public OperationResult SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var result = new OperationResult();
            if (fromPlayerId == toPlayerId)
                return Fail("No puedes agregarte a ti mismo.");

            using (var db = new codenamesEntities())
            {
                var alreadyExists = db.Friendships.Any(f =>
                    (f.playerID == fromPlayerId && f.friendID == toPlayerId) ||
                    (f.playerID == toPlayerId && f.friendID == fromPlayerId));

                if (alreadyExists)
                    return Fail("Ya existe una relación o solicitud entre estos jugadores.");

                db.Friendships.Add(new Friendship
                {
                    playerID = fromPlayerId,
                    friendID = toPlayerId,
                    requestStatus = false
                });

                db.SaveChanges();
                return Ok("Solicitud enviada.");
            }
        }

        public OperationResult AcceptFriendRequest(Guid playerId, Guid requesterPlayerId)
        {
            using (var db = new codenamesEntities())
            {
                var req = db.Friendships
                    .FirstOrDefault(f => f.playerID == requesterPlayerId &&
                                         f.friendID == playerId &&
                                         f.requestStatus == false);

                if (req == null)
                    return Fail("No se encontró la solicitud pendiente.");

                req.requestStatus = true;

                var reciprocalExists = db.Friendships.Any(f =>
                    f.playerID == playerId && f.friendID == requesterPlayerId && f.requestStatus == true);

                if (!reciprocalExists)
                {
                    db.Friendships.Add(new Friendship
                    {
                        playerID = playerId,
                        friendID = requesterPlayerId,
                        requestStatus = true
                    });
                }

                db.SaveChanges();
                return Ok("Solicitud aceptada.");
            }
        }

        public OperationResult RejectFriendRequest(Guid playerId, Guid requesterPlayerId)
        {
            using (var db = new codenamesEntities())
            {
                var req = db.Friendships
                    .FirstOrDefault(f => f.playerID == requesterPlayerId &&
                                         f.friendID == playerId &&
                                         f.requestStatus == false);

                if (req == null)
                    return Fail("No se encontró la solicitud pendiente.");

                db.Friendships.Remove(req);
                db.SaveChanges();
                return Ok("Solicitud rechazada.");
            }
        }

        public OperationResult RemoveFriend(Guid playerId, Guid friendId)
        {
            using (var db = new codenamesEntities())
            {
                var links = db.Friendships.Where(f =>
                    (f.playerID == playerId && f.friendID == friendId && f.requestStatus == true) ||
                    (f.playerID == friendId && f.friendID == playerId && f.requestStatus == true)).ToList();

                if (!links.Any())
                    return Fail("No existe una relación de amistad aceptada.");

                db.Friendships.RemoveRange(links);
                db.SaveChanges();
                return Ok("Amigo eliminado.");
            }
        }

        private static OperationResult Ok(string msg) => new OperationResult { Success = true, Message = msg };
        private static OperationResult Fail(string msg) => new OperationResult { Success = false, Message = msg };
    }
}