using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
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
    }
}