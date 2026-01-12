using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Operations
{
    public static class FriendCallbackManager
    {
        private static readonly Dictionary<Guid, IFriendCallback> _clients = new Dictionary<Guid, IFriendCallback>();

        public static void Register(Guid playerId, IFriendCallback callback)
        {
            if (playerId == Guid.Empty)
            {
                return;
            }

            lock (_clients)
            {
                _clients[playerId] = callback;
            }
        }

        public static void Unregister(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return;
            }

            lock (_clients)
            {
                if (_clients.ContainsKey(playerId))
                {
                    _clients.Remove(playerId);
                }
            }
        }

        public static IFriendCallback GetCallback(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return null;
            }

            lock (_clients)
            {
                _clients.TryGetValue(playerId, out IFriendCallback callback);
                return callback;
            }
        }

        public static void InvokeCallback(Guid playerId, Action<IFriendCallback> action)
        {
            var callback = GetCallback(playerId);
            if (callback != null)
            {
                try
                {
                    action(callback);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    Unregister(playerId);
                    ServerLogger.Log.Warn("Could not send notification to friend service client:", ex);
                }
                catch (Exception ex)
                {
                    Unregister(playerId);
                    ServerLogger.Log.Error("Unexpected exception trying to send notification to friend service client: ", ex);
                }
            }
        }
    }
}