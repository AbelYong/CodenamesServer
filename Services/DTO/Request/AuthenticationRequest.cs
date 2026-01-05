using System;
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by AuthenticationService to hold an account's UserID,
    /// where a valid UserID is interpreted as a successful authentication
    /// </summary>
    [DataContract]
    public class AuthenticationRequest : Request
    {
        [DataMember]
        public Guid? UserID { get; set; }

        [DataMember]
        public DateTimeOffset? BanExpiration { get; set; }

        public AuthenticationRequest()
        {
            UserID = Guid.Empty;
            BanExpiration = null;
        }
    }
}
