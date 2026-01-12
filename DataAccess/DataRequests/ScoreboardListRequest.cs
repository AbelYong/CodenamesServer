using DataAccess.DataRequests;
using System.Collections.Generic;

namespace DataAccess.DataRequests
{
    public class ScoreboardListRequest : DataRequest
    {
        public List<Scoreboard> Scoreboards { get; set; }

        public ScoreboardListRequest()
        {
            Scoreboards = new List<Scoreboard>();
        }
    }
}