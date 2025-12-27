using log4net;
using Services.DTO;

namespace Services.Operations
{
    public static class ServerLogger
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(ServerLogger));

        public static string GetPlayerIdentifier(Player player)
        {
            if (player == null)
            {
                return "WARNING: identification requested for NULL reference player object";
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
    }
}
