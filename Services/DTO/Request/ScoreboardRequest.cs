using System.Collections.Generic;
using System.Linq; // Necesario para SequenceEqual
using System.Runtime.Serialization;
using Services.DTO.DataContract;

namespace Services.DTO.Request
{
    [DataContract]
    public class ScoreboardRequest : Request
    {
        [DataMember]
        public List<Scoreboard> ScoreboardList { get; set; }

        public ScoreboardRequest()
        {
            ScoreboardList = new List<Scoreboard>();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScoreboardRequest other)
            {
                if (!IsSuccess.Equals(other.IsSuccess) || !StatusCode.Equals(other.StatusCode))
                {
                    return false;
                }
                if (ScoreboardList == null && other.ScoreboardList == null) return true;
                if (ScoreboardList == null || other.ScoreboardList == null) return false;
                if (ScoreboardList.Count != other.ScoreboardList.Count) return false;

                return ScoreboardList.SequenceEqual(other.ScoreboardList);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode, ScoreboardList }.GetHashCode();
        }
    }
}