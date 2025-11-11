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

            List<ISessionCallback> friendChannels = friendCallbacks.Values.ToList();
            NotifyOnlineFriends(friendChannels, player);

            List<Player> onlineFriends = friendCallbacks.Keys.ToList();
            currentClientChannel.ReceiveOnlineFriends(onlineFriends);
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

        private static void NotifyOnlineFriends(List<ISessionCallback> friendChannels, Player player)
        {
            foreach (ISessionCallback friendChannel in friendChannels)
            {
                friendChannel.NotifyFriendOnline(player);
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

            List<ISessionCallback> onlineFriendsChannels = friendCallbacks.Values.ToList();
            foreach (ISessionCallback friendChannel in onlineFriendsChannels)
            {
                friendChannel.NotifyFriendOffline(playerID);
            }
            System.Console.WriteLine("{0} has disconnected", player.Username);
        }
    }
}
