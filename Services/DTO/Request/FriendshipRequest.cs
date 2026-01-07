using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class FriendshipRequest : Request
    {
        public override bool Equals(object obj)
        {
            if (obj is FriendshipRequest other)
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
