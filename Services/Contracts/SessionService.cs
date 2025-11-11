using Services.DTO;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services.Contracts
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class SessionService : ISessionManager
    {
        private static FriendService friendService = new FriendService();
        private readonly Dictionary<Player, ISessionCallback> _playersOnline = new Dictionary<Player, ISessionCallback>();
        
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
                if (_playersOnline.ContainsKey(player))
                {
                    request.IsSuccess = false;
                    request.StatusCode= StatusCode.UNAUTHORIZED;
                    return request;
                }
                else
                {
                    _playersOnline.Add(player, currentClientChannel);

                    playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
                }
            }

            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnline(friends, playersOnlineSnapshot);
            NotifyConnectToOnlineFriends(friendCallbacks, player);
            KeyValuePair<Player, ISessionCallback> playerCallback = new KeyValuePair<Player, ISessionCallback> (player, currentClientChannel);
            SendOnlineFriends(friends, playerCallback);

            request.IsSuccess = true;
            request.StatusCode = StatusCode.OK;
            System.Console.WriteLine("{0} has connected", player.Username);
            return request;
        }

        private static Dictionary<Player, ISessionCallback> GetFriendsOnline(List<Player> friends, Dictionary<Player, ISessionCallback> playersOnline)
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

            lock (_playersOnline)
            {
                if (_playersOnline.Remove(player))
                {
                    playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
                }
                else
                {
                    return;
                }
            }

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnline(friends, playersOnlineSnapshot);
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

        private void RemoveFaultedChannels(Dictionary<Player, ISessionCallback> faultedChannels)
        {
            bool channelsFaulted = faultedChannels.Count > 0;
            if (channelsFaulted)
            {
                lock (_playersOnline)
                {
                    foreach (KeyValuePair<Player, ISessionCallback> friendChannel in faultedChannels)
                    {
                        _playersOnline.Remove(friendChannel.Key);
                    }
                }
            }
        }
    }
}
