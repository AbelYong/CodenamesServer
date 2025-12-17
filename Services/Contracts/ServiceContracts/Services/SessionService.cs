using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class SessionService : ISessionManager
    {
        private static readonly Lazy<SessionService> _instance = new Lazy<SessionService>(() => new SessionService());
        private readonly IFriendManager _friendService;
        private readonly ICallbackProvider _callbackProvider;
        private readonly ConcurrentDictionary<Player, ISessionCallback> _playersOnline;

        public static SessionService Instance
        {
            get => _instance.Value;
        }

        public SessionService() : this (new FriendService(), new CallbackProvider())
        {
            _playersOnline = new ConcurrentDictionary<Player, ISessionCallback>();
        }

        public SessionService(IFriendManager friendService, ICallbackProvider callbackProvider)
        {
            _friendService = friendService;
            _callbackProvider = callbackProvider;
            _playersOnline = new ConcurrentDictionary<Player, ISessionCallback>();
        }

        public CommunicationRequest Connect(Player player)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (player == null || !player.PlayerID.HasValue)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }

            var currentClientChannel = _callbackProvider.GetCallback<ISessionCallback>();

            Guid playerID = (Guid)player.PlayerID;

            Dictionary<Player, ISessionCallback> playersOnlineSnapshot = new Dictionary<Player, ISessionCallback>();

            lock (_playersOnline)
            {
                if (!_playersOnline.TryAdd(player, currentClientChannel))
                {
                    // If player is currently online, kick them from their previous login
                    // wether they'll be allowed to connect is determined by if the kick was sucessful
                    request.IsSuccess = HandleDuplicateLogin(player, currentClientChannel);
                    if (!request.IsSuccess)
                    {
                        request.StatusCode = StatusCode.UNAUTHORIZED;
                        return request;
                    }
                }

                playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
            }

            List<Player> friends = _friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnlineChannels(friends, playersOnlineSnapshot);
            NotifyConnectToOnlineFriends(friendCallbacks, player);

            KeyValuePair<Player, ISessionCallback> playerCallback = new KeyValuePair<Player, ISessionCallback> (player, currentClientChannel);
            List<Player> onlineFriends = friendCallbacks.Keys.ToList();
            SendOnlineFriends(onlineFriends, playerCallback);

            request.IsSuccess = true;
            request.StatusCode = StatusCode.OK;
            string playerOnlineMessage = string.Format("{0} has connected", player.PlayerID);
            ServerLogger.Log.Info(message:playerOnlineMessage);
            return request;
        }

        private bool HandleDuplicateLogin(Player player, ISessionCallback channel)
        {
            Guid playerID = (Guid)player.PlayerID;
            KickPlayer(playerID, KickReason.DUPLICATE_LOGIN);
            var kickedPlayer = _playersOnline.FirstOrDefault(p => p.Key.PlayerID == playerID);
            if (kickedPlayer.Key == null)
            {
                return _playersOnline.TryAdd(player, channel);
            }
            return false;
        }

        private static Dictionary<Player, ISessionCallback> GetFriendsOnlineChannels(List<Player> friends, Dictionary<Player, ISessionCallback> playersOnline)
        {
            HashSet<Player> friendSet = new HashSet<Player>(friends);

            return playersOnline
                .Where(player => friendSet.Contains(player.Key))
                .ToDictionary(player => player.Key, player => player.Value);
        }

        private void NotifyConnectToOnlineFriends(Dictionary<Player, ISessionCallback> friendCallbacks, Player player)
        {
            Dictionary<Player, ISessionCallback> faultedChannels = new Dictionary<Player, ISessionCallback>();
            foreach (KeyValuePair<Player, ISessionCallback> friendChannel in friendCallbacks)
            {
                try
                {
                    friendChannel.Value.NotifyFriendOnline(player);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    faultedChannels.Add(friendChannel.Key, friendChannel.Value);
                    if (friendChannel.Value is ICommunicationObject communicationObject)
                    {
                        communicationObject.Abort();
                    }
                    ServerLogger.Log.Warn("Exception while notifying friend online to a player: ", ex);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while notifying friend online to a player: ", ex);
                } 
            }
            RemoveFaultedChannels(faultedChannels);
        }

        private void SendOnlineFriends(List<Player> onlineFriends, KeyValuePair<Player, ISessionCallback> playerCallback)
        {
            try
            {
                playerCallback.Value.ReceiveOnlineFriends(onlineFriends);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                if (playerCallback.Value is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
                _playersOnline.TryRemove(playerCallback.Key, out _);
                ServerLogger.Log.Warn("Exception while sending online friends to a player: ", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending online friends to a player: ", ex);
                _playersOnline.TryRemove(playerCallback.Key, out _);
            }
        }

        public void Disconnect(Player player)
        {
            if (player == null || !player.PlayerID.HasValue)
            {
                return;
            }

            Guid playerID = (Guid)player.PlayerID;
            List<Player> friends = _friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> playersOnlineSnapshot = new Dictionary<Player, ISessionCallback>();

            lock (_playersOnline)
            {
                if (_playersOnline.TryRemove(player, out ISessionCallback _))
                {
                    playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
                }
                else
                {
                    return;
                }
            }

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnlineChannels(friends, playersOnlineSnapshot);
            NotifyDisconnectToOnlineFriends(friendCallbacks, playerID);
            string playerOfflineMessage = string.Format("{0} has disconnected", player.PlayerID);
            ServerLogger.Log.Info(message: playerOfflineMessage);
        }

        private void NotifyDisconnectToOnlineFriends(Dictionary<Player, ISessionCallback> friendCallbacks, Guid playerID)
        {
            Dictionary<Player, ISessionCallback> faultedChannels = new Dictionary<Player, ISessionCallback>();
            foreach (KeyValuePair<Player, ISessionCallback> friendChannel in friendCallbacks)
            {
                try
                {
                    friendChannel.Value.NotifyFriendOffline(playerID);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    faultedChannels.Add(friendChannel.Key, friendChannel.Value);
                    if (friendChannel.Value is ICommunicationObject communicationObject)
                    {
                        communicationObject.Abort();
                    }
                    ServerLogger.Log.Warn("Exception while sending disconnect notification: ", ex);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while sending disconnect notification: ", ex);
                }
            }
            RemoveFaultedChannels(faultedChannels);
        }

        public void NotifyNewFriendship(Player friendA, Player friendB)
        {
            bool isAOnline = _playersOnline.TryGetValue(friendA, out ISessionCallback channelA);
            bool isBOnline = _playersOnline.TryGetValue(friendB, out ISessionCallback channelB);

            if (isAOnline)
            {
                SendNewFriendshipNotification(channelA, friendA, friendB);
            }

            if (isBOnline)
            {
                SendNewFriendshipNotification(channelB, friendB, friendA);
            }
        }

        private void SendNewFriendshipNotification(ISessionCallback toNotifyChannel, Player toNotifyPlayer, Player newFriend)
        {
            try
            {
                toNotifyChannel.NotifyFriendOnline(newFriend);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                RemoveFaultedChannel(toNotifyPlayer);
                ServerLogger.Log.Warn("Exception while sending new friendship notification:", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending new friendship notification: ", ex);
            }
        }

        public void NotifyFriendshipEnded(Player friendA, Player friendB)
        {
            bool isAOnline = _playersOnline.TryGetValue(friendA, out ISessionCallback channelA);
            bool isBOnline = _playersOnline.TryGetValue(friendB, out ISessionCallback channelB);

            if (isAOnline)
            {
                SendFriendshipEndedNotification(channelA, friendA, friendB);
            }

            if (isBOnline)
            {
                SendFriendshipEndedNotification(channelB, friendB, friendA);
            }
        }

        private void SendFriendshipEndedNotification(ISessionCallback toNotifyChannel, Player toNotifyPlayer, Player formerFriend)
        {
            try
            {
                toNotifyChannel.NotifyFriendOffline(formerFriend.PlayerID.Value);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                RemoveFaultedChannel(toNotifyPlayer);
                ServerLogger.Log.Warn("Exception while sending friendship ended notification:", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending friendship ended notification: ", ex);
            }
        }

        private void RemoveFaultedChannel(Player player)
        {
            if (_playersOnline.TryGetValue(player, out ISessionCallback faultedChannel))
            {
                KeyValuePair<Player, ISessionCallback> entryToRemove =
                    new KeyValuePair<Player, ISessionCallback>(player, faultedChannel);

                bool removed = ((ICollection<KeyValuePair<Player, ISessionCallback>>)_playersOnline)
                    .Remove(entryToRemove);

                if (removed && faultedChannel is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
            }
        }

        private void RemoveFaultedChannels(Dictionary<Player, ISessionCallback> faultedChannels)
        {
            var collection = (ICollection<KeyValuePair<Player, ISessionCallback>>)_playersOnline;

            foreach (KeyValuePair<Player, ISessionCallback> failedEntry in faultedChannels)
            {
                if (failedEntry.Value is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
                collection.Remove(failedEntry);
            }
        }
        
        public void KickPlayer(Guid playerID, KickReason reason)
        {
            var onlinePlayer = _playersOnline.FirstOrDefault(p => p.Key.PlayerID == playerID);

            if (onlinePlayer.Key != null)
            {
                ISessionCallback callback = onlinePlayer.Value;
                try
                {
                    callback.NotifyKicked(reason);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    RemoveFaultedChannel(onlinePlayer.Key);
                    ServerLogger.Log.Warn("Exception while sending kick notification:", ex);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while sending kick notification:", ex);
                }
                Disconnect(onlinePlayer.Key);
            }
        }

        public bool IsPlayerOnline(Guid playerId)
        {
            return _playersOnline.Keys.Any(p => p.PlayerID == playerId);
        }
    }
}