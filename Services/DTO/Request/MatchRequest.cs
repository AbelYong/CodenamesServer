using Services.DTO.DataContract;
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class MatchRequest : Request
    {
        [DataMember]
        public Match Match { get; set; }
    }
}
