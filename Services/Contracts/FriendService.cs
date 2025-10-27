using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using DataAccess.Users;
using Services.DTO;

namespace Services
{
    public class FriendService : IFriendManager
    {
        private static IFriendDAO _friendDAO = new FriendDAO();

        public List<Player> SearchPlayers(string query, Guid mePlayerId, int limit)
        {
            var items = _friendDAO.SearchPlayers(query, mePlayerId, limit <= 0 ? 20 : limit);
            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }

        public List<Player> GetFriends(Guid mePlayerId)
        {
            var items = _friendDAO.GetFriends(mePlayerId);
            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }

        public List<Player> GetIncomingRequests(Guid mePlayerId)
        {
            var items = _friendDAO.GetIncomingRequests(mePlayerId);
            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }

        public OperationResultSv SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var r = _friendDAO.SendFriendRequest(fromPlayerId, toPlayerId);
            return new OperationResultSv { Success = r.Success, Message = r.Message };
        }

        public OperationResultSv AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var r = _friendDAO.AcceptFriendRequest(mePlayerId, requesterPlayerId);
            return new OperationResultSv { Success = r.Success, Message = r.Message };
        }

        public OperationResultSv RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var r = _friendDAO.RejectFriendRequest(mePlayerId, requesterPlayerId);
            return new OperationResultSv { Success = r.Success, Message = r.Message };
        }

        public OperationResultSv RemoveFriend(Guid mePlayerId, Guid friendPlayerId)
        {
            var r = _friendDAO.RemoveFriend(mePlayerId, friendPlayerId);
            return new OperationResultSv { Success = r.Success, Message = r.Message };
        }
    }
}