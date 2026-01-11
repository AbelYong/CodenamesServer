using log4net;
using Services.Contracts.ServiceContracts.Managers;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using System;

namespace Services.Operations
{
    public static class ServerLogger
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(ServerLogger));
        private static readonly ISessionManager _sessionService = SessionService.Instance;

        public static string GetPlayerIdentifier(Guid playerID)
        {
            Player player = GetPlayer(playerID);
            if (player.PlayerID == Guid.Empty)
            {
                return "WARNING: identification requested for player with Empty playerID";
            }
            if (!player.PlayerID.HasValue)
            {
                if (string.IsNullOrEmpty(player.Username))
                {
                    return "playerID NULL and username NULL or empty";
                }
                else
                {
                    string message = string.Format("playerID NULL, username {0}", player.Username);
                    return message;
                }
            }

            if (player.IsGuest)
            {
                return player.Username;
            }
            else
            {
                return player.PlayerID.ToString();
            }
        }

        private static Player GetPlayer(Guid playerID)
        {
            Player player = _sessionService.GetOnlinePlayer(playerID);
            if (player == null)
            {
                return new Player { PlayerID = Guid.Empty };
            }
            return player;
        }
    }
}
