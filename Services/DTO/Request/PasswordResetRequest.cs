using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by AuthenticationService to report the amount of attempts remaning after an attempted password reset
    /// </summary>
    [DataContract]
    public class PasswordResetRequest : Request
    {
        [DataMember]
        public int RemainingAttempts { get; set; }
    }
}
