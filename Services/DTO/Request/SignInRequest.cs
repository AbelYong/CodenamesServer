using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class SignInRequest : Request
    {
        [DataMember]
        public bool IsUsernameDuplicate { get; set; }
        [DataMember]
        public bool IsEmailDuplicate { get; set; }
        [DataMember]
        public bool IsEmailValid { get; set; }
        [DataMember]
        public bool IsPasswordValid { get; set; }
    }
}
