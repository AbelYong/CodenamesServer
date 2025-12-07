using System;
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class AuthenticationRequest : Request
    {
        [DataMember]
        public Guid? UserID { get; set; }
        public AuthenticationRequest()
        {
            UserID = Guid.Empty;
        }
    }
}
