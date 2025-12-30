using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
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
