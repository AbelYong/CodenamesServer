using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Represents registred player's records.
    /// FastestMatch only counts Matches that were won
    /// </summary>
    [DataContract]
    public class Scoreboard
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int GamesWon { get; set; }

        [DataMember]
        public string FastestMatch { get; set; }

        [DataMember]
        public int AssassinsRevealed { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Scoreboard other)
            {
                return Username == other.Username &&
                       GamesWon == other.GamesWon &&
                       FastestMatch == other.FastestMatch &&
                       AssassinsRevealed == other.AssassinsRevealed;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { Username, GamesWon, FastestMatch, AssassinsRevealed }.GetHashCode();
        }
    }
}