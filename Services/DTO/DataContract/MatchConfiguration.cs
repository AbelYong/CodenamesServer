using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used to Generate matches according to the provided MatchRules,
    /// Requester and Companion (and their playerIDs) are required
    /// </summary>
    [DataContract]
    public class MatchConfiguration
    {
        [DataMember]
        public Player Requester { get; set; }

        [DataMember]
        public Player Companion { get; set; }

        [DataMember]
        public MatchRules MatchRules { get; set; }
    }
}
