using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    [KnownType(typeof(CommunicationRequest))]
    [KnownType(typeof(LoginRequest))]
    [KnownType(typeof(MatchRequest))]
    public abstract class Request
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        
        [DataMember]
        public StatusCode StatusCode { get; set; }
    }
}
