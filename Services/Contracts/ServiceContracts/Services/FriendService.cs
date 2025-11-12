using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using DataAccess.Users;
using Services.DTO;
using Services.Operations;

namespace Services
{
    /// <summary>
    /// Implementation of the friends service.
    /// Configured as PerSession so that each client has its own instance.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class FriendService : IFriendManager
    {
        private static readonly IFriendDAO _friendDAO = new FriendDAO();

        private static readonly IPlayerDAO _playerDAO = new PlayerDAO();

        private Guid _playerId;
        private IFriendCallback _callback;

        public void Connect(Guid mePlayerId)
        {
            if (mePlayerId == Guid.Empty)
            {
                return;
            }

            _playerId = mePlayerId;
            _callback = OperationContext.Current.GetCallbackChannel<IFriendCallback>();

            FriendCallbackManager.Register(_playerId, _callback);

            var commObject = (ICommunicationObject)_callback;
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

        public void SendFriendRequest(Guid fromPlayerId, Guid toPlayerId)
        {
            var result = _friendDAO.SendFriendRequest(fromPlayerId, toPlayerId);

            if (result.Success)
            {
                var fromPlayer = _playerDAO.GetPlayerById(fromPlayerId);

                var playerDto = Player.AssembleSvPlayer(fromPlayer);

                FriendCallbackManager.InvokeCallback(toPlayerId,
                    c => c.NotifyNewFriendRequest(playerDto));

                _callback.NotifyOperationSuccess(result.Message);
            }
            else
            {
                _callback.NotifyOperationFailure(result.Message);
            }
        }

        public void AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var result = _friendDAO.AcceptFriendRequest(mePlayerId, requesterPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);

                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(requesterPlayerId,
                    c => c.NotifyFriendRequestAccepted(playerDto));

                _callback.NotifyOperationSuccess(result.Message);
            }
            else
            {
                _callback.NotifyOperationFailure(result.Message);
            }
        }

        public void RejectFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var result = _friendDAO.RejectFriendRequest(mePlayerId, requesterPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);

                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(requesterPlayerId,
                    c => c.NotifyFriendRequestRejected(playerDto));

                _callback.NotifyOperationSuccess(result.Message);
            }
            else
            {
                _callback.NotifyOperationFailure(result.Message);
            }
        }

        public void RemoveFriend(Guid mePlayerId, Guid friendPlayerId)
        {
            var result = _friendDAO.RemoveFriend(mePlayerId, friendPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerDAO.GetPlayerById(mePlayerId);

                var playerDto = Player.AssembleSvPlayer(mePlayer);

                FriendCallbackManager.InvokeCallback(friendPlayerId,
                    c => c.NotifyFriendRemoved(playerDto));

                _callback.NotifyOperationSuccess(result.Message);
            }
            else
            {
                _callback.NotifyOperationFailure(result.Message);
            }
        }

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
    }
}