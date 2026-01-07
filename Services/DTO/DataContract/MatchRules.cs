using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Allows for the configuration of a Match. For Gamemodes Normal and Counterintelligence
    /// only Gamemode is required, for Custom mode values for TurnTimer, TimerTokens
    /// and BystanderTokens must be provided
    /// </summary>
    [DataContract]
    public class MatchRules
    {
        public const int NORMAL_TURN_TIMER = 30;
        public const int NORMAL_TIMER_TOKENS = 9;
        public const int NORMAL_BYSTANDER_TOKENS = 0;

        public const int COUNTERINT_TURN_TIMER = 45;
        public const int COUNTERINT_TIMER_TOKENS = 12;
        public const int COUNTERINT_BYSTANDER_TOKENS = 0;
        public const int COUNTERINT_ASSASSINS = 16;

        public const int MAX_TURN_TIMER = 60;
        public const int MAX_TIMER_TOKENS = 12;
        public const int MAX_BYSTANDER_TOKENS = 13;
        public const int NORMAL_MAX_ASSASSINS = 3;

        public const int TIMER_TOKENS_TO_TAKE_NON_CUSTOM = 1;
        public const int TIMER_TOKENS_TO_TAKE_CUSTOM = 2;

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

        public override bool Equals(object obj)
        {
            if (obj is MatchRules other)
            {
                return
                    Gamemode.Equals(other.Gamemode) &&
                    TurnTimer.Equals(other.TurnTimer) &&
                    TimerTokens.Equals(other.TimerTokens) &&
                    BystanderTokens.Equals(other.BystanderTokens) &&
                    MaxAssassins.Equals(other.MaxAssassins);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new
            {
                Gamemode, TurnTimer, TimerTokens, BystanderTokens, MaxAssassins
            }.GetHashCode();
        }
    }
}
