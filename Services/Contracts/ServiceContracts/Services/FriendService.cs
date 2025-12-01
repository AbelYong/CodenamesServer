using DataAccess.Users;
using Services.Contracts;
using Services.DTO;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services
{
    /// <summary>
    /// Implementation of the friends service.
    /// Configured as PerSession so that each client has its own instance.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class FriendService : IFriendManager
    {
        private readonly ICallbackProvider _callbackProvider;
        private readonly IFriendDAO _friendDAO;
        private readonly IPlayerDAO _playerDAO;
        private Guid _playerId;

        public FriendService() : this (new FriendDAO(), new PlayerDAO(), new CallbackProvider()) { }

        public FriendService(IFriendDAO friendDAO, IPlayerDAO playerDAO, ICallbackProvider callbackProvider)
        {
            _friendDAO = friendDAO;
            _playerDAO = playerDAO;
            _callbackProvider = callbackProvider;
        }

        public void Connect(Guid mePlayerId)
        {
            if (mePlayerId == Guid.Empty)
            {
                return;
            }

            _playerId = mePlayerId;
            IFriendCallback callback = _callbackProvider.GetCallback<IFriendCallback>();

            FriendCallbackManager.Register(_playerId, callback);
            Console.WriteLine("{0} registered to friend service", mePlayerId);

            var commObject = (ICommunicationObject)callback;
            commObject.Closing += OnClientClosing;
            commObject.Faulted += OnClientClosing;
        }

        private void OnClientClosing(object sender, EventArgs e)
        {
            FriendCallbackManager.Unregister(_playerId);
        }

        public void Disconnect(Guid mePlayerId)
        {
            FriendCallbackManager.Unregister(mePlayerId);
        }

        public FriendshipRequest SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var response = new FriendshipRequest();

            if (fromPlayerId == toPlayerId)
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.UNALLOWED;
                return response;
            }

            var result = _friendDAO.SendFriendRequest(fromPlayerId, toPlayerId);

            if (result.Success)
            {
                var fromPlayer = _playerDAO.GetPlayerById(fromPlayerId);
                var playerDto = Player.AssembleSvPlayer(fromPlayer);

                FriendCallbackManager.InvokeCallback(toPlayerId,
                    c => c.NotifyNewFriendRequest(playerDto));

                response.IsSuccess = true;
                response.StatusCode = StatusCode.FRIEND_REQUEST_SENT;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.CONFLICT;
            }

            return response;
        }

        public FriendshipRequest AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var response = new FriendshipRequest();
            var result = _friendDAO.AcceptFriendRequest(mePlayerId, requesterPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);
                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(requesterPlayerId,
                    c => c.NotifyFriendRequestAccepted(playerDto));

                response.IsSuccess = true;
                response.StatusCode = StatusCode.FRIEND_ADDED;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.SERVER_ERROR;
            }

            return response;
        }

        public FriendshipRequest RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var response = new FriendshipRequest();
            var result = _friendDAO.RejectFriendRequest(mePlayerId, requesterPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);
                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(requesterPlayerId,
                    c => c.NotifyFriendRequestRejected(playerDto));

                response.IsSuccess = true;
                response.StatusCode = StatusCode.FRIEND_REQUEST_REJECTED;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.SERVER_ERROR;
            }

            return response;
        }

        public FriendshipRequest RemoveFriend(Guid mePlayerId, Guid friendPlayerId)
        {
            var response = new FriendshipRequest();
            var result = _friendDAO.RemoveFriend(mePlayerId, friendPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);
                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(friendPlayerId,
                    c => c.NotifyFriendRemoved(playerDto));

                response.IsSuccess = true;
                response.StatusCode = StatusCode.FRIEND_REMOVED;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.SERVER_ERROR;
            }

            return response;
        }

        public List<Player> SearchPlayers(string query, Guid mePlayerId, int limit)
        {
            var items = _friendDAO.SearchPlayers(query, mePlayerId, limit <= 0 ? 20 : limit);

            if (items == null)
            {
                return new List<Player>();
            }

            return items
                .Where(p => p.playerID != mePlayerId)
                .Select(Player.AssembleSvPlayer)
                .Where(p => p != null)
                .ToList();
        }

        public List<Player> GetFriends(Guid mePlayerId)
        {
            var items = _friendDAO.GetFriends(mePlayerId);

            if (items == null)
            {
                return new List<Player>();
            }

            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }

        public List<Player> GetIncomingRequests(Guid mePlayerId)
        {
            var items = _friendDAO.GetIncomingRequests(mePlayerId);

            if (items == null)
            {
                return new List<Player>();
            }

            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }

        public List<Player> GetSentRequests(Guid mePlayerId)
        {
            var items = _friendDAO.GetSentRequests(mePlayerId);

            if (items == null)
            {
                return new List<Player>();
            }

            return items.Select(Player.AssembleSvPlayer).Where(p => p != null).ToList();
        }
    }
}