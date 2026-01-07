using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by UserService to specify why a Sign-In attempt failed
    /// </summary>
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

        public override bool Equals(object obj)
        {
            if (obj is  SignInRequest other)
            {
                return
                    IsSuccess.Equals(other.IsSuccess) &&
                    StatusCode.Equals(other.StatusCode) &&
                    IsUsernameDuplicate.Equals(other.IsUsernameDuplicate) &&
                    IsEmailDuplicate.Equals(other.IsEmailDuplicate) &&
                    IsEmailValid.Equals(other.IsEmailValid) &&
                    IsPasswordValid.Equals(other.IsPasswordValid);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode, IsUsernameDuplicate, IsEmailDuplicate, IsEmailValid, IsPasswordValid }.GetHashCode();
        }
    }
}
