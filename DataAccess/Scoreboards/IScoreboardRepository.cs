using System;
using System.Collections.Generic;
using DataAccess.DataRequests;

namespace DataAccess.Scoreboards
{
    public interface IScoreboardRepository
    {
        UpdateRequest UpdateMatchesWon(Guid playerID);
        UpdateRequest UpdateFastestMatchRecord(Guid playerID,TimeSpan matchLength);
        UpdateRequest UpdateAssassinsPicked(Guid playerID);
        ScoreboardListRequest GetPlayerScoreboard(Guid playerID);
        ScoreboardListRequest GetTopPlayersByWins(int topCount);
    }
}
