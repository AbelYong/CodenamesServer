using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class PasswordResetRequest : Request
    {
        [DataMember]
        public int RemainingAttempts { get; set; }
    }
}
