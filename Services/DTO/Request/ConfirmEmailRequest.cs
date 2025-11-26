
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class ConfirmEmailRequest : Request
    {
        [DataMember]
        public int RemainingAttempts { get; set; }
    }
}
