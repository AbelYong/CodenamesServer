using Services.DTO.DataContract;
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class JoinPartyRequest : Request
    {
        [DataMember]
        public Party Party { get; set; }
    }
}
