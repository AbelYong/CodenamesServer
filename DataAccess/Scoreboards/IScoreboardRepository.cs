using System;
using System.Collections.Generic;
using DataAccess.DataRequests;

namespace DataAccess.Scoreboards
{
    public interface IScoreboardRepository
    {
        bool UpdateMatchesWon(Guid playerID);

        bool UpdateFastestMatchRecord(Guid playerID,TimeSpan matchLength);
        
        bool UpdateAssassinsPicked(Guid playerID);
        Scoreboard GetPlayerScoreboard(Guid playerID);
        ScoreboardListRequest GetTopPlayersByWins(int topCount);
    }
}
