using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    [KnownType(typeof(LoginRequest))]
    public abstract class Request
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        
        [DataMember]
        public StatusCode StatusCode { get; set; }
    }
}
