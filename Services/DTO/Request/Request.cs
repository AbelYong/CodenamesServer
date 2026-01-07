using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Base class to wrap requests to the server, providing a quick Success/Failure indicator (IsSuccess),
    /// and a specific Status to provide more specific error information
    /// </summary>
    [DataContract]
    [KnownType(typeof(CommunicationRequest))]
    [KnownType(typeof(AuthenticationRequest))]
    [KnownType(typeof(SignInRequest))]
    [KnownType(typeof(ConfirmEmailRequest))]
    [KnownType(typeof(CreateLobbyRequest))]
    [KnownType(typeof(JoinPartyRequest))]
    [KnownType(typeof(FriendshipRequest))]
    [KnownType(typeof(PasswordResetRequest))]
    public abstract class Request
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        
        [DataMember]
        public StatusCode StatusCode { get; set; }

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }
}
