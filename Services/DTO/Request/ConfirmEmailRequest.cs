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

        public override bool Equals(object obj)
        {
            if (obj is ConfirmEmailRequest other)
            {
                return
                    IsSuccess.Equals(other.IsSuccess) &&
                    StatusCode.Equals(other.StatusCode) &&
                    RemainingAttempts.Equals(other.RemainingAttempts);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode, RemainingAttempts }.GetHashCode();
        }
    }
}
