using DataAccess.Scoreboards;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ScoreboardService : IScoreboardManager
    {
        private readonly IScoreboardDAO _scoreboard;
        private static readonly List<IScoreboardCallback> _connectedClients = new List<IScoreboardCallback>();

        public ScoreboardService()
        {
            _scoreboard = new ScoreboardDAO();
        }

        public static void NotifyMatchConcluded()
        {
            ScoreboardDAO dao = new ScoreboardDAO();
            try
            {
                var topPlayers = GetTopPlayers(dao);
                NotifyAllClients(topPlayers);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Error when attempting to notify the conclusion of the game in ScoreboardService.", ex);
            }
        }

        public void SubscribeToScoreboardUpdates(Guid playerID)
        {
            try
            {
                IScoreboardCallback callback = OperationContext.Current.GetCallbackChannel<IScoreboardCallback>();
                lock (_connectedClients)
                {
                    if (!_connectedClients.Contains(callback))
                    {
                        _connectedClients.Add(callback);
                    }
                }
                var currentTop = GetTopPlayers(_scoreboard);
                callback.NotifyLeaderboardUpdate(currentTop);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error($"Error subscribing player {playerID} to the scoreboard.", ex);
            }
        }

        public void UnsubscribeFromScoreboardUpdates(Guid playerID)
        {
            try
            {
                IScoreboardCallback callback = OperationContext.Current.GetCallbackChannel<IScoreboardCallback>();
                lock (_connectedClients)
                {
                    if (_connectedClients.Contains(callback))
                    {
                        _connectedClients.Remove(callback);
                    }
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error($"Error unsubscribing player {playerID} from the scoreboard.", ex);
            }
        }

        public Scoreboard GetMyScore(Guid playerID)
        {
            try
            {
                var s = _scoreboard.GetPlayerScoreboard(playerID);
                if (s == null) return null;

                return new Scoreboard
                {
                    Username = s.Player != null ? s.Player.username : "Unknown",
                    GamesWon = s.mostGamesWon ?? 0,
                    FastestMatch = s.fastestGame.HasValue ? s.fastestGame.Value.ToString(@"mm\:ss") : "--:--",
                    AssassinsRevealed = s.assassinsRevealed ?? 0
                };
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error($"Error obtaining the personal score of player {playerID}.", ex);
                return null;
            }
        }

        private static List<Scoreboard> GetTopPlayers(IScoreboardDAO dao)
        {
            var scoreboards = dao.GetTopPlayersByWins(10);
            return scoreboards.Select(s => new Scoreboard
            {
                Username = s.Player != null ? s.Player.username : "Unknown",
                GamesWon = s.mostGamesWon ?? 0,
                FastestMatch = s.fastestGame.HasValue ? s.fastestGame.Value.ToString(@"mm\:ss") : "--:--",
                AssassinsRevealed = s.assassinsRevealed ?? 0
            }).ToList();
        }

        private static void NotifyAllClients(List<Scoreboard> data)
        {
            lock (_connectedClients)
            {
                for (int i = _connectedClients.Count - 1; i >= 0; i--)
                {
                    var client = _connectedClients[i];
                    try
                    {
                        if (((ICommunicationObject)client).State == CommunicationState.Opened)
                        {
                            client.NotifyLeaderboardUpdate(data);
                        }
                        else
                        {
                            _connectedClients.RemoveAt(i);
                        }
                    }
                    catch (Exception)
                    {
                        _connectedClients.RemoveAt(i);
                    }
                }
            }
        }
    }
}