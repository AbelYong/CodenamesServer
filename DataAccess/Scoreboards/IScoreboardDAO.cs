using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Scoreboards
{
    public interface IScoreboardDAO
    {
        bool UpdateMatchesWon(Guid playerID);

        bool UpdateFastestMatchRecord(Guid playerID,TimeSpan matchLength);
        
        bool UpdateAssassinsPicked(Guid playerID);
        Scoreboard GetPlayerScoreboard(Guid playerID);
        List<Scoreboard> GetTopPlayersByWins(int topCount);
    }
}
