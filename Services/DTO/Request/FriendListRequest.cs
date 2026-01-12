using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Services.DTO.DataContract;

namespace Services.DTO.Request
{
    [DataContract]
    public class FriendListRequest : Request
    {
        [DataMember]
        public List<Player> FriendsList { get; set; }

        public FriendListRequest()
        {
            FriendsList = new List<Player>();
        }

        public override bool Equals(object obj)
        {
            if (obj is FriendListRequest other)
            {
                if (!IsSuccess.Equals(other.IsSuccess) || !StatusCode.Equals(other.StatusCode))
                {
                    return false;
                }
                if (FriendsList == null && other.FriendsList == null) return true;
                if (FriendsList == null || other.FriendsList == null) return false;
                return FriendsList.SequenceEqual(other.FriendsList);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode, FriendsList }.GetHashCode();
        }
    }
}