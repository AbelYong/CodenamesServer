using DataAccess.Users;
using Services.Contracts;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class FriendService : IFriendManager
    {
        private readonly ICallbackProvider _callbackProvider;
        private readonly IFriendRepository _friendDAO;
        private readonly IPlayerRepository _playerRepository;
        private Guid _playerId;

        public FriendService() : this (new CallbackProvider(), new FriendRepository(), new PlayerRepository()) { }

        public FriendService(ICallbackProvider callbackProvider, IFriendRepository friendDAO, IPlayerRepository playerRepository)
        {
            _callbackProvider = callbackProvider;
            _friendDAO = friendDAO;
            _playerRepository = playerRepository;
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
                var fromPlayer = _playerRepository.GetPlayerById(fromPlayerId);
                var playerDto = Player.AssembleSvPlayer(fromPlayer);

                FriendCallbackManager.InvokeCallback(toPlayerId,
                    c => c.NotifyNewFriendRequest(playerDto));

                response.IsSuccess = true;
                response.StatusCode = StatusCode.FRIEND_REQUEST_SENT;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = StatusCode.SERVER_ERROR;
            }

            return response;
        }

        public FriendshipRequest AcceptFriendRequest(Guid mePlayerId, Guid requesterPlayerId)
        {
            var response = new FriendshipRequest();
            var result = _friendDAO.AcceptFriendRequest(mePlayerId, requesterPlayerId);

            if (result.Success)
            {
                var mePlayer = _playerRepository.GetPlayerById(mePlayerId);
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
                var mePlayer = _playerRepository.GetPlayerById(mePlayerId);
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
                var mePlayer = _playerRepository.GetPlayerById(mePlayerId);
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

        public FriendListRequest SearchPlayers(string query, Guid mePlayerId, int limit)
        {
            FriendListRequest response = new FriendListRequest();

            var result = _friendDAO.SearchPlayers(query, mePlayerId, limit <= 0 ? 20 : limit);

            if (result.IsSuccess)
            {
                response.IsSuccess = true;
                response.StatusCode = StatusCode.OK;

                response.FriendsList = result.Players
                    .Where(p => p.playerID != mePlayerId)
                    .Select(global::Services.DTO.DataContract.Player.AssembleSvPlayer)
                    .Where(p => p != null)
                    .ToList();
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = GetStatusCodeFromDbError(result.ErrorType);
                response.FriendsList = new List<Player>();
            }

            return response;
        }

        public FriendListRequest GetFriends(Guid mePlayerId)
        {
            FriendListRequest response = new FriendListRequest();
            var result = _friendDAO.GetFriends(mePlayerId);

            if (result.IsSuccess)
            {
                response.IsSuccess = true;
                response.StatusCode = StatusCode.OK;

                response.FriendsList = result.Players
                    .Select(global::Services.DTO.DataContract.Player.AssembleSvPlayer)
                    .Where(p => p != null)
                    .ToList();
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = GetStatusCodeFromDbError(result.ErrorType);
                response.FriendsList = new List<Player>();
            }

            return response;
        }

        public FriendListRequest GetIncomingRequests(Guid mePlayerId)
        {
            FriendListRequest response = new FriendListRequest();
            var result = _friendDAO.GetIncomingRequests(mePlayerId);

            if (result.IsSuccess)
            {
                response.IsSuccess = true;
                response.StatusCode = StatusCode.OK;

                response.FriendsList = result.Players
                    .Select(Player.AssembleSvPlayer)
                    .Where(p => p != null)
                    .ToList();
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = GetStatusCodeFromDbError(result.ErrorType);
                response.FriendsList = new List<Player>();
            }

            return response;
        }

        public FriendListRequest GetSentRequests(Guid mePlayerId)
        {
            FriendListRequest response = new FriendListRequest();
            var result = _friendDAO.GetSentRequests(mePlayerId);

            if (result.IsSuccess)
            {
                response.IsSuccess = true;
                response.StatusCode = StatusCode.OK;

                response.FriendsList = result.Players
                    .Select(global::Services.DTO.DataContract.Player.AssembleSvPlayer)
                    .Where(p => p != null)
                    .ToList();
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = GetStatusCodeFromDbError(result.ErrorType);
                response.FriendsList = new List<global::Services.DTO.DataContract.Player>();
            }

            return response;
        }

        private static StatusCode GetStatusCodeFromDbError(DataAccess.DataRequests.ErrorType errorType)
        {
            if (errorType == DataAccess.DataRequests.ErrorType.DB_ERROR)
            {
                return StatusCode.DATABASE_ERROR;
            }
            return StatusCode.SERVER_ERROR;
        }
    }
}