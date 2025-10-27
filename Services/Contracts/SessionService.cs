using Services.DTO;
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
        private readonly Dictionary<Player, ISocialCallback> _playersOnline = new Dictionary<Player, ISocialCallback>();
        
        public void Connect(Player player)
        {
            if (player == null || !player.PlayerID.HasValue)
            {
                return;
            }

            var currentClientChannel = OperationContext.Current.GetCallbackChannel<ISocialCallback>();

            Guid playerID = (Guid)player.PlayerID;
            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISocialCallback> playersOnlineSnapshot = new Dictionary<Player, ISocialCallback>();

            lock (_playersOnline)
            {
                if (_playersOnline.ContainsKey(player))
                {
                    //TODO handle duplicated logins
                }
                else
                {
                    _playersOnline.Add(player, currentClientChannel);

                    playersOnlineSnapshot = _playersOnline.ToDictionary(session => session.Key, session => session.Value);
                }
            }

            Dictionary<Player, ISocialCallback> friendCallbacks = GetFriendsOnline(friends, playersOnlineSnapshot);

            List<ISocialCallback> friendChannels = friendCallbacks.Values.ToList();
            foreach (ISocialCallback friendChannel in friendChannels)
            {
                friendChannel.NotifyFriendOnline(player);
            }

            List<Player> onlineFriends = friendCallbacks.Keys.ToList();
            currentClientChannel.ReceiveOnlineFriends(onlineFriends);
        }

        public void Disconnect(Player player)
        {
            if (player == null || !player.PlayerID.HasValue)
            {
                return;
            }

            Guid playerID = (Guid)player.PlayerID;
            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISocialCallback> playersOnlineSnapshot = new Dictionary<Player, ISocialCallback>();

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

            Dictionary<Player, ISocialCallback> friendCallbacks = GetFriendsOnline(friends, playersOnlineSnapshot);

            List<ISocialCallback> onlineFriendsChannels = friendCallbacks.Values.ToList();
            foreach (ISocialCallback friendChannel in onlineFriendsChannels)
            {
                friendChannel.NotifyFriendOffline(playerID);
            }
        }

        private static Dictionary<Player, ISocialCallback> GetFriendsOnline(List<Player> friends, Dictionary<Player, ISocialCallback> playersOnline)
        {
            Dictionary<Player, ISocialCallback> friendCallbacks = new Dictionary<Player, ISocialCallback>();
            
            HashSet<Player> friendSet = new HashSet<Player>(friends);
            
            foreach (var playerOnline in playersOnline)
            {
                if (friendSet.Contains(playerOnline.Key))
                {
                    friendCallbacks.Add(playerOnline.Key, playerOnline.Value);
                }
            }
            return friendCallbacks;
        }
    }
}
