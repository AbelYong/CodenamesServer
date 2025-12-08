using DataAccess.Scoreboards;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ScoreboardService : IScoreboardManager
    {
        private readonly IScoreboardDAO _scoreboard;
        private readonly ICallbackProvider _callbackProvider;
        private static readonly ConcurrentDictionary<Guid, IScoreboardCallback> _connectedClients = 
            new ConcurrentDictionary<Guid, IScoreboardCallback>();

        public ScoreboardService() : this (new ScoreboardDAO(), new CallbackProvider()) { }

        public ScoreboardService(IScoreboardDAO scoreboardDAO, ICallbackProvider callbackProvider)
        {
            _callbackProvider = callbackProvider;
            _scoreboard = scoreboardDAO;
        }

        public void NotifyMatchConcluded()
        {
            var topPlayers = GetTopPlayers();
            NotifyAllClients(topPlayers);
        }

        public void SubscribeToScoreboardUpdates(Guid playerID)
        {
            try
            {
                IScoreboardCallback callback = _callbackProvider.GetCallback<IScoreboardCallback>();
                if (_connectedClients.TryAdd(playerID, callback))
                {
                    var currentTop = GetTopPlayers();
                    callback.NotifyLeaderboardUpdate(currentTop);
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                ServerLogger.Log.Warn($"Failed to suscribe player {playerID} to the scoreboard: ", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error($"Unexpected exception while suscribing player {playerID} to Scoreboard service: ", ex);
            }
        }

        public void UnsubscribeFromScoreboardUpdates(Guid playerID)
        {
            bool playerDisconnect = _connectedClients.TryRemove(playerID, out IScoreboardCallback callback);
            if (playerDisconnect && callback is ICommunicationObject communicationObject)
            {
                communicationObject.Close();
            }
        }

        public Scoreboard GetMyScore(Guid playerID)
        {
            var myScore = _scoreboard.GetPlayerScoreboard(playerID);
            if (myScore == null)
            {
                return null;
            }

            return new Scoreboard
            {
                Username = myScore.Player != null ? myScore.Player.username : "Unknown",
                GamesWon = myScore.mostGamesWon ?? 0,
                FastestMatch = myScore.fastestGame.HasValue ? myScore.fastestGame.Value.ToString(@"mm\:ss") : "--:--",
                AssassinsRevealed = myScore.assassinsRevealed ?? 0
            };
        }

        private List<Scoreboard> GetTopPlayers()
        {
            var scoreboards = _scoreboard.GetTopPlayersByWins(10);
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
                foreach (KeyValuePair<Guid, IScoreboardCallback> client in _connectedClients)
                {
                    try
                    {
                        if (((ICommunicationObject)client.Value).State == CommunicationState.Opened)
                        {
                            client.Value.NotifyLeaderboardUpdate(data);
                        }
                        else
                        {
                            _connectedClients.TryRemove(client.Key, out _);
                        }
                    }
                    catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                    {
                        ServerLogger.Log.Warn("Failed to notify leaderboard update after game conclusion in ScoreboardService: ", ex);
                        _connectedClients.TryRemove(client.Key, out _);
                        RemoveFaultedChannel(client.Value);
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.Log.Error("Unexpected exception when attempting to notify the conclusion of the game in ScoreboardService: ", ex);
                        _connectedClients.TryRemove(client.Key, out _);
                        RemoveFaultedChannel(client.Value);
                    }
                }
            }
        }

        private static void RemoveFaultedChannel(IScoreboardCallback faultedChannel)
        {
            if (faultedChannel is ICommunicationObject communicationObject)
            {
                communicationObject.Abort();
            }
        }
    }
}