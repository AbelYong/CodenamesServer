using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    [KnownType(typeof(CommunicationRequest))]
    [KnownType(typeof(LoginRequest))]
    [KnownType(typeof(CreateLobbyRequest))]
    public abstract class Request
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        
        [DataMember]
        public StatusCode StatusCode { get; set; }
    }
}
