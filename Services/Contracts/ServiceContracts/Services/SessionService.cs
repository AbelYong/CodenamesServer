using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.Request;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class SessionService : ISessionManager
    {
        public static SessionService Instance { get; private set; }

        private static readonly FriendService friendService = new FriendService();
        private readonly ConcurrentDictionary<Player, ISessionCallback> _playersOnline = new ConcurrentDictionary<Player, ISessionCallback>();
        
        public SessionService()
        {
            Instance = this;
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

            var currentClientChannel = OperationContext.Current.GetCallbackChannel<ISessionCallback>();

            Guid playerID = (Guid)player.PlayerID;

            Dictionary<Player, ISessionCallback> playersOnlineSnapshot = new Dictionary<Player, ISessionCallback>();

            lock (_playersOnline)
            {
                if (!_playersOnline.TryAdd(player, currentClientChannel))
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                    return request;
                }

                playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
            }

            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnlineChannels(friends, playersOnlineSnapshot);
            NotifyConnectToOnlineFriends(friendCallbacks, player);
            KeyValuePair<Player, ISessionCallback> playerCallback = new KeyValuePair<Player, ISessionCallback> (player, currentClientChannel);
            List<Player> onlineFriends = friendCallbacks.Keys.ToList();
            SendOnlineFriends(onlineFriends, playerCallback);

            request.IsSuccess = true;
            request.StatusCode = StatusCode.OK;
            System.Console.WriteLine("{0} has connected", player.Username);
            return request;
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
                catch (Exception ex) when (ex is CommunicationException || ex is EndpointNotFoundException || ex is CommunicationObjectFaultedException)
                {
                    faultedChannels.Add(friendChannel.Key, friendChannel.Value);
                    if (friendChannel.Value is ICommunicationObject communicationObject)
                    {
                        communicationObject.Abort();
                    }
                }
            }
            RemoveFaultedChannels(faultedChannels);
        }

        private static void SendOnlineFriends(List<Player> onlineFriends, KeyValuePair<Player, ISessionCallback> playerCallback)
        {
            try
            {
                playerCallback.Value.ReceiveOnlineFriends(onlineFriends);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is EndpointNotFoundException || ex is CommunicationObjectFaultedException)
            {
                if (playerCallback.Value is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
            }
        }

        public void Disconnect(Player player)
        {
            if (player == null || !player.PlayerID.HasValue)
            {
                return;
            }

            Guid playerID = (Guid)player.PlayerID;
            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> playersOnlineSnapshot = new Dictionary<Player, ISessionCallback>();

            ISessionCallback removedClientChannel;
            lock (_playersOnline)
            {
                if (_playersOnline.TryRemove(player, out removedClientChannel))
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

            System.Console.WriteLine("{0} has disconnected", player.Username);
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
                catch (Exception ex) when (ex is CommunicationException || ex is EndpointNotFoundException || ex is CommunicationObjectFaultedException)
                {
                    faultedChannels.Add(friendChannel.Key, friendChannel.Value);
                    if (friendChannel.Value is ICommunicationObject communicationObject)
                    {
                        communicationObject.Abort();
                    }
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
                try
                {
                    channelA.NotifyFriendOnline(friendB);
                }
                catch (CommunicationException)
                {
                    RemoveFaultedChannel(friendA);
                }
            }

            if (isBOnline)
            {
                try
                {
                    channelB.NotifyFriendOnline(friendA);
                }
                catch (CommunicationException)
                {
                    RemoveFaultedChannel(friendB);
                }
            }
        }

        public void NotifyFriendshipEnded(Player friendA, Player friendB)
        {
            bool isAOnline = _playersOnline.TryGetValue(friendA, out ISessionCallback channelA);
            bool isBOnline = _playersOnline.TryGetValue(friendB, out ISessionCallback channelB);

            if (isAOnline)
            {
                try
                {
                    channelA.NotifyFriendOffline(friendB.PlayerID.Value);
                }
                catch (CommunicationException)
                {
                    RemoveFaultedChannel(friendA);
                }
            }

            if (isBOnline)
            {
                try
                {
                    channelB.NotifyFriendOffline(friendA.PlayerID.Value);
                }
                catch (CommunicationException)
                {
                    RemoveFaultedChannel(friendB);
                }
            }
        }

        private void RemoveFaultedChannel(Player player)
        {
            if (_playersOnline.TryGetValue(player, out ISessionCallback faultedChannel))
            {
                KeyValuePair<Player, ISessionCallback> entryToRemove = new KeyValuePair<Player, ISessionCallback>(player, faultedChannel);

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
                collection.Remove(failedEntry);
            }
        }

        /// <summary>
        /// Allows other services (like ModerationService) to kick a connected user.
        /// </summary>
        public void KickUser(Guid userID, BanReason reason)
        {
            var entry = _playersOnline.FirstOrDefault(p => p.Key.PlayerID == userID);

            if (entry.Key != null)
            {
                ISessionCallback callback = entry.Value;
                try
                {
                    callback.NotifyKicked(reason);
                }
                catch (CommunicationException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending kick notification: {ex.Message}");
                }

                Disconnect(entry.Key);
            }
        }

        /// <summary>
        /// Checks if a player is currently logged in by their ID.
        /// </summary>
        public bool IsPlayerOnline(Guid playerId)
        {
            return _playersOnline.Keys.Any(p => p.PlayerID == playerId);
        }
    }
}