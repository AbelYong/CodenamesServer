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
    public class ScoreboardDAO : IScoreboardDAO
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IPlayerDAO _playerDAO;

        public ScoreboardDAO() : this(new DbContextFactory(), new PlayerDAO()) { }

        public ScoreboardDAO(IDbContextFactory contextFactory, IPlayerDAO playerDAO)
        {
            _contextFactory = contextFactory;
            _playerDAO = playerDAO;
        }

        public bool UpdateMatchesWon(Guid playerID)
        {
            if (_playerDAO.VerifyIsPlayerGuest(playerID))
            {
                return true;
            }

            try
            {
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
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.mostGamesWon = 1;
                        scoreboard.assassinsRevealed = 0;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update number of matches won: ", ex);
                return false;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating number of matches won: ", ex);
                return false;
            }
        }

        public bool UpdateFastestMatchRecord(Guid playerID, TimeSpan matchLength)
        {
            if (_playerDAO.VerifyIsPlayerGuest(playerID))
            {
                return true;
            }

            try
            {
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
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.fastestGame = matchLength;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update fastest match record: ", ex);
                return false;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating fastest match record: ", ex);
                return false;
            }
        }

        public bool UpdateAssassinsPicked(Guid playerID)
        {
            if (_playerDAO.VerifyIsPlayerGuest(playerID))
            {
                return true;
            }

            try
            {
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
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.assassinsRevealed = 1;
                        scoreboard.mostGamesWon = 0;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Error("Failed to update number of assassins picked: ", ex);
                return false;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while updating number of assassins picked: ", ex);
                return false;
            }
        }

        public Scoreboard GetPlayerScoreboard(Guid playerID)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    return context.Scoreboards.Include("Player")
                                  .FirstOrDefault(s => s.playerID == playerID);
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Failed to retrieve player scoreboard", ex);
                return null;
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception retrieving player scoreboard: ", ex);
                return null;
            }
        }

        public List<Scoreboard> GetTopPlayersByWins(int topCount)
        {
            try
            {
                using (var context = _contextFactory.Create())
                {
                    return context.Scoreboards.Include("Player")
                                  .OrderByDescending(s => s.mostGamesWon)
                                  .Take(topCount)
                                  .ToList();
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                DataAccessLogger.Log.Debug("Exception while retrieving top players", ex);
                return new List<Scoreboard>();
            }
            catch (Exception ex)
            {
                DataAccessLogger.Log.Error("Unexpected exception while retrieving top players: ", ex);
                return new List<Scoreboard>();
            }
        }
    }
}
