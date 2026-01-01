
namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by both MatchmakingService and MatchService to determine which
    /// match rules should be followed
    /// </summary>
    public enum Gamemode
    {
        NORMAL,
        CUSTOM,
        COUNTERINTELLIGENCE
    }
}
