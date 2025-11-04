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
        private readonly Dictionary<Player, ISessionCallback> _playersOnline = new Dictionary<Player, ISessionCallback>();
        
        public void Connect(Player player)
        {
            if (player == null || !player.PlayerID.HasValue)
            {
                return;
            }

            var currentClientChannel = OperationContext.Current.GetCallbackChannel<ISessionCallback>();

            Guid playerID = (Guid)player.PlayerID;
            List<Player> friends = friendService.GetFriends(playerID);

            Dictionary<Player, ISessionCallback> playersOnlineSnapshot = new Dictionary<Player, ISessionCallback>();

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

            Dictionary<Player, ISessionCallback> friendCallbacks = GetFriendsOnline(friends, playersOnlineSnapshot);

            List<ISessionCallback> friendChannels = friendCallbacks.Values.ToList();
            foreach (ISessionCallback friendChannel in friendChannels)
            {
                friendChannel.NotifyFriendOnline(player);
            }

            List<Player> onlineFriends = friendCallbacks.Keys.ToList();
            currentClientChannel.ReceiveOnlineFriends(onlineFriends);
            System.Console.WriteLine("{0} has connected", player.Username);
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

            List<ISessionCallback> onlineFriendsChannels = friendCallbacks.Values.ToList();
            foreach (ISessionCallback friendChannel in onlineFriendsChannels)
            {
                friendChannel.NotifyFriendOffline(playerID);
            }
            System.Console.WriteLine("{0} has disconnected", player.Username);
        }

        private static Dictionary<Player, ISessionCallback> GetFriendsOnline(List<Player> friends, Dictionary<Player, ISessionCallback> playersOnline)
        {
            Dictionary<Player, ISessionCallback> friendCallbacks = new Dictionary<Player, ISessionCallback>();
            
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
