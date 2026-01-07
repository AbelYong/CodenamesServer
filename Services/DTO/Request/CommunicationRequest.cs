using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// General request used when the Success or failure of the operation, (plus additional information through the stautus code)
    /// is the only information needed by the client
    /// </summary>
    [DataContract]
    public class CommunicationRequest : Request
    {
        public override bool Equals(object obj)
        {
            if (obj is  CommunicationRequest other)
            {
                return 
                    IsSuccess.Equals(other.IsSuccess) &&
                    StatusCode.Equals(other.StatusCode);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode }.GetHashCode();
        }
    }
}
