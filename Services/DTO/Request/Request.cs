using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    [KnownType(typeof(CommunicationRequest))]
    [KnownType(typeof(LoginRequest))]
    [KnownType(typeof(SignInRequest))]
    [KnownType(typeof(ConfirmEmailRequest))]
    [KnownType(typeof(CreateLobbyRequest))]
    [KnownType(typeof(JoinPartyRequest))]
    public abstract class Request
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        
        [DataMember]
        public StatusCode StatusCode { get; set; }
    }
}
