using System;
using System.Collections.Generic;
using System.ServiceModel;
using Services.Contracts;

namespace Services.Operations
{
    /// <summary>
    /// Statically manages active callback channels for the FriendService.
    /// This allows PerSession instances of the service to notify each other.
    /// </summary>
    public static class FriendCallbackManager
    {
        private static readonly Dictionary<Guid, IFriendCallback> _clients =
            new Dictionary<Guid, IFriendCallback>();

        /// <summary>
        /// Registers a new client (player) and its callback channel.
        /// </summary>
        /// <param name="playerId">The ID of the player connecting.</param>
        /// <param name="callback">The client's callback channel.</param>
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

        /// <summary>
        /// Removes a client from the registry, typically when disconnecting.
        /// </summary>
        /// <param name="playerId">The ID of the player to unregister.</param>
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

        /// <summary>
        /// Gets the callback channel for a specific player, if it exists.
        /// </summary>
        /// <param name="playerId">The ID of the player to notify.</param>
        /// <returns>The callback channel, or null if not connected.</returns>
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

        /// <summary>
        /// Safely executes an action in a player's callback.
        /// Handles the case where the player is not connected.
        /// </summary>
        /// <param name="playerId">The ID of the player to notify.</param>
        /// <param name="action">The action to execute in their callback channel.</param>
        public static void InvokeCallback(Guid playerId, Action<IFriendCallback> action)
        {
            var callback = GetCallback(playerId);
            if (callback != null)
            {
                try
                {
                    action(callback);
                }
                catch (Exception)
                {
                    Unregister(playerId);
                }
            }
        }
    }
}