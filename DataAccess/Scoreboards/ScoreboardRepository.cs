using DataAccess.DataRequests;
using DataAccess.Users;
using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccess.Scoreboards
{
    public class ScoreboardRepository : IScoreboardRepository
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IPlayerRepository _playerRepository;

        public ScoreboardRepository() : this(new DbContextFactory(), new PlayerRepository()) { }

        public ScoreboardRepository(IDbContextFactory contextFactory, IPlayerRepository playerRepository)
        {
            _contextFactory = contextFactory;
            _playerRepository = playerRepository;
        }

        public UpdateRequest UpdateMatchesWon(Guid playerID)
        {
            UpdateRequest dataRequest = new UpdateRequest();
            try
            {
                if (_playerRepository.VerifyIsPlayerGuest(playerID))
                {
                    dataRequest.IsSuccess = true;
                    return dataRequest;
                }

                using (var context = _contextFactory.Create())
                {
                    var query = from s in context.Scoreboards
                                where s.playerID == playerID
                                select s;
                    Scoreboard scoreboard = query.FirstOrDefault();

                    if (scoreboard != null)
                    {
                        scoreboard.mostGamesWon = (scoreboard.mostGamesWon ?? 0) + 1;
                        context.SaveChanges();
                        dataRequest.IsSuccess = true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.mostGamesWon = 1;
                        scoreboard.assassinsRevealed = 0;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        dataRequest.IsSuccess = true;
                    }
                    return dataRequest;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update number of matches won: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating number of matches won: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
        }

        public UpdateRequest UpdateFastestMatchRecord(Guid playerID, TimeSpan matchLength)
        {
            UpdateRequest dataRequest = new UpdateRequest();
            try
            {
                if (_playerRepository.VerifyIsPlayerGuest(playerID))
                {
                    dataRequest.IsSuccess = true;
                    return dataRequest;
                }

                using (var context = _contextFactory.Create())
                {
                    var query = from s in context.Scoreboards
                                where s.playerID == playerID
                                select s;
                    Scoreboard scoreboard = query.FirstOrDefault();
                    if (scoreboard != null)
                    {
                        if (scoreboard.fastestGame.HasValue)
                        {
                            TimeSpan fastestGame = (TimeSpan)scoreboard.fastestGame;
                            if (matchLength.CompareTo(fastestGame) < 0)
                            {
                                scoreboard.fastestGame = matchLength;
                                context.SaveChanges();
                            }
                        }
                        else
                        {
                            scoreboard.fastestGame = matchLength;
                            context.SaveChanges();
                        }
                        dataRequest.IsSuccess = true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.fastestGame = matchLength;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        dataRequest.IsSuccess = true;
                    }
                    return dataRequest;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update fastest match record: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating fastest match record: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
        }

        public UpdateRequest UpdateAssassinsPicked(Guid playerID)
        {
            UpdateRequest dataRequest = new UpdateRequest();
            try
            {
                if (_playerRepository.VerifyIsPlayerGuest(playerID))
                {
                    dataRequest.IsSuccess = true;
                    return dataRequest;
                }

                using (var context = _contextFactory.Create())
                {
                    var query = from s in context.Scoreboards
                                where s.playerID == playerID
                                select s;
                    Scoreboard scoreboard = query.FirstOrDefault();

                    if (scoreboard != null)
                    {
                        scoreboard.assassinsRevealed = (scoreboard.assassinsRevealed ?? 0) + 1;
                        context.SaveChanges();
                        dataRequest.IsSuccess = true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.assassinsRevealed = 1;
                        scoreboard.mostGamesWon = 0;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        dataRequest.IsSuccess = true;
                    }
                    return dataRequest;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Failed to update number of assassins picked: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating number of assassins picked: ", ex);
                dataRequest.IsSuccess = false;
                dataRequest.ErrorType = ErrorType.DB_ERROR;
                return dataRequest;
            }
        }

        public ScoreboardListRequest GetPlayerScoreboard(Guid playerID)
        {
            ScoreboardListRequest result = new ScoreboardListRequest();
            result.Scoreboards = new List<Scoreboard>();

            try
            {
                using (var context = _contextFactory.Create())
                {
                    var scoreboard = context.Scoreboards.Include("Player")
                                            .FirstOrDefault(s => s.playerID == playerID);

                    if (scoreboard != null)
                    {
                        result.Scoreboards.Add(scoreboard);
                        result.IsSuccess = true;
                    }
                    else
                    {
                        result.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Error($"DB Error retrieving scoreboard for player {playerID}", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error($"Unexpected error retrieving scoreboard for player {playerID}", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
            }

            return result;
        }

        public ScoreboardListRequest GetTopPlayersByWins(int topCount)
        {
            ScoreboardListRequest result = new ScoreboardListRequest();
            try
            {
                using (var context = _contextFactory.Create())
                {
                    var list = context.Scoreboards.Include("Player")
                                  .OrderByDescending(s => s.mostGamesWon)
                                  .Take(topCount)
                                  .ToList();

                    result.Scoreboards = list;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while retrieving top players", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
                result.Scoreboards = new List<Scoreboard>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while retrieving top players: ", ex);
                result.IsSuccess = false;
                result.ErrorType = ErrorType.DB_ERROR;
                result.Scoreboards = new List<Scoreboard>();
            }
            return result;
        }
    }
}
