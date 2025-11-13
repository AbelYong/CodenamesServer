using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class MatchRules
    {
        [DataMember]
        public Gamemode Gamemode { get; set; }

        [DataMember]
        public int TurnTimer { get; set; }

        [DataMember]
        public int TimerTokens { get; set; }

        [DataMember]
        public int BystanderTokens { get; set; }

        public int MaxAssassins { get; set; }

        public void SetMaxAssassins(int maxAssassins)
        {
            const int MIN_ASSASSINS = 3;
            MaxAssassins = maxAssassins > MIN_ASSASSINS ? maxAssassins : MIN_ASSASSINS;
        }
    }
}
