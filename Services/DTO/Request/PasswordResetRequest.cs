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

        public override bool Equals(object obj)
        {
            if (obj is PasswordResetRequest other)
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
