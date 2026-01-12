using DataAccess.Scoreboards;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.DTO.Request;
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
        private readonly IScoreboardRepository _scoreboard;
        private readonly ICallbackProvider _callbackProvider;
        private static readonly ConcurrentDictionary<Guid, IScoreboardCallback> _connectedClients = 
            new ConcurrentDictionary<Guid, IScoreboardCallback>();

        public ScoreboardService() : this (new ScoreboardRepository(), new CallbackProvider()) { }

        public ScoreboardService(IScoreboardRepository scoreboardDAO, ICallbackProvider callbackProvider)
        {
            _callbackProvider = callbackProvider;
            _scoreboard = scoreboardDAO;
        }

        public void NotifyMatchConcluded()
        {
            var response = GetTopPlayers();
            if (response.IsSuccess)
            {
                NotifyAllClients(response.ScoreboardList);
            }
            else
            {
                ServerLogger.Log.Warn("Could not notify match conclusion due to DB error fetching top players.");
            }
        }

        public void SubscribeToScoreboardUpdates(Guid playerID)
        {
            try
            {
                IScoreboardCallback callback = _callbackProvider.GetCallback<IScoreboardCallback>();
                if (_connectedClients.TryAdd(playerID, callback))
                {
                    var response = GetTopPlayers();

                    if (response.IsSuccess)
                    {
                        callback.NotifyLeaderboardUpdate(response.ScoreboardList);
                    }
                    else
                    {
                        string message = string.Format("Could not send initial scoreboard to player {0} due to DB error.", playerID);
                        ServerLogger.Log.Warn(message);
                    }
                }
                string audit = string.Format("{0} has suscribed to Scoreboard Service", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
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

                string audit = string.Format("{0} has unsuscribed from Scoreboard Service", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
            }
        }

        public ScoreboardRequest GetMyScore(Guid playerID)
        {
            ScoreboardRequest response = new ScoreboardRequest();
            var daoResult = _scoreboard.GetPlayerScoreboard(playerID);

            if (daoResult.IsSuccess)
            {
                var myScoreEntity = daoResult.Scoreboards.FirstOrDefault();

                if (myScoreEntity != null)
                {
                    response.IsSuccess = true;
                    response.StatusCode = StatusCode.OK;

                    Scoreboard dto = new Scoreboard
                    {
                        Username = myScoreEntity.Player != null ? myScoreEntity.Player.username : "Unknown",
                        GamesWon = myScoreEntity.mostGamesWon ?? 0,
                        FastestMatch = myScoreEntity.fastestGame.HasValue ? myScoreEntity.fastestGame.Value.ToString(@"mm\:ss") : "--:--",
                        AssassinsRevealed = myScoreEntity.assassinsRevealed ?? 0
                    };

                    response.ScoreboardList = new List<Scoreboard> { dto };
                }
                else
                {
                    response.IsSuccess = false;
                    response.StatusCode = StatusCode.NOT_FOUND;
                    response.ScoreboardList = new List<Scoreboard>();
                }
            }
            else
            {
                response.IsSuccess = false;
                response.ScoreboardList = new List<Scoreboard>();
                response.StatusCode = GetStatusCodeFromDbError(daoResult.ErrorType);

                ServerLogger.Log.Warn($"Failed to get score for player {playerID}. ErrorType: {daoResult.ErrorType}");
            }

            return response;
        }

        public ScoreboardRequest GetTopPlayers()
        {
            ScoreboardRequest response = new ScoreboardRequest();
            var daoResult = _scoreboard.GetTopPlayersByWins(10);

            if (daoResult.IsSuccess)
            {
                response.IsSuccess = true;
                response.StatusCode = StatusCode.OK;

                List<Scoreboard> list = new List<Scoreboard>();
                foreach (var item in daoResult.Scoreboards)
                {
                    Scoreboard dto = new Scoreboard();
                    dto.Username = item.Player != null ? item.Player.username : "Unknown";
                    dto.GamesWon = item.mostGamesWon ?? 0;
                    dto.AssassinsRevealed = item.assassinsRevealed ?? 0;
                    dto.FastestMatch = item.fastestGame.HasValue ? item.fastestGame.Value.ToString(@"hh\:mm\:ss") : "N/A";
                    list.Add(dto);
                }
                response.ScoreboardList = list;
            }
            else
            {
                response.IsSuccess = false;
                response.StatusCode = GetStatusCodeFromDbError(daoResult.ErrorType);
                response.ScoreboardList = new List<Scoreboard>();
            }

            return response;
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

        private static StatusCode GetStatusCodeFromDbError(DataAccess.DataRequests.ErrorType errorType)
        {
            if (errorType == DataAccess.DataRequests.ErrorType.DB_ERROR)
            {
                return StatusCode.DATABASE_ERROR;
            }
            return StatusCode.SERVER_ERROR;
        }
    }
}