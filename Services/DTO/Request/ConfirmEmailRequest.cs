using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by EmailService to specify the amount of reamining attempts after trying to confirm an Email/Password reset
    /// </summary>
    [DataContract]
    public class ConfirmEmailRequest : Request
    {
        [DataMember]
        public int RemainingAttempts { get; set; }
    }
}
