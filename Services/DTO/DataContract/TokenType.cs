
namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by MatchService to specify to Spymasters the type of token to update after a Bystander selection.
    /// Test is for testing purposes, it shouldn't be used or seen by the client
    /// </summary>
    public enum TokenType
    {
        TIMER,
        BYSTANDER,
        TEST
    }
}
