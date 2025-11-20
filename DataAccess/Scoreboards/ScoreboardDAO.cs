using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Scoreboards
{
    public class ScoreboardDAO : IScoreboardDAO
    {
        public bool UpdateMatchesWon(Guid playerID)
        {
            try
            {
                using (var context = new codenamesEntities())
                {
                    var query = from s in context.Scoreboards
                                where s.playerID == playerID
                                select s;
                    Scoreboard scoreboard = query.FirstOrDefault();
                    if (scoreboard != null)
                    {
                        scoreboard.mostGamesWon++;
                        context.SaveChanges();
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.mostGamesWon = 1;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update number of assassins picked: ", ex);
                return false;
            }
        }

        public bool UpdateFastestMatchRecord(Guid playerID, TimeSpan matchLength)
        {
            try
            {
                using (var context = new codenamesEntities())
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
                            }
                        }
                        else
                        {
                            scoreboard.fastestGame = matchLength;
                        }
                        context.SaveChanges();
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.assassinsRevealed = 1;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update fastest game: ", ex);
                return false;
            }
        }

        public bool UpdateAssassinsPicked(Guid playerID)
        {
            try
            {
                using (var context = new codenamesEntities())
                {
                    var query = from s in context.Scoreboards
                                where s.playerID == playerID
                                select s;
                    Scoreboard scoreboard = query.FirstOrDefault();
                    if (scoreboard != null)
                    {
                        scoreboard.assassinsRevealed++;
                        context.SaveChanges();
                        return true;
                    }
                    else
                    {
                        scoreboard = new Scoreboard();
                        scoreboard.playerID = playerID;
                        scoreboard.assassinsRevealed = 1;
                        context.Scoreboards.Add(scoreboard);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                DataAccessLogger.Log.Warn("Failed to update number of assassins picked", ex);
                return false;
            }
        }
    }
}
