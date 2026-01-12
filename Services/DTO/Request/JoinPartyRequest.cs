using Services.DTO.DataContract;
using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by LobbyService to send the data of the existing party back to the client who wants to join a party
    /// </summary>
    [DataContract]
    public class JoinPartyRequest : Request
    {
        [DataMember]
        public Party Party { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is JoinPartyRequest other)
            {
                return EqualityHelper(other);
            }
            return false;
        }

        private bool EqualityHelper(JoinPartyRequest other)
        {
            if (!EvaluateNullEqualiy(other))
            {
                return false;
            }

            return
                IsSuccess.Equals(other.IsSuccess) &&
                StatusCode.Equals(other.StatusCode) &&
                Party.PartyHost.Equals(other.Party.PartyHost) &&
                Party.PartyGuest.Equals(other.Party.PartyGuest) &&
                Party.LobbyCode.Equals(other.Party.LobbyCode);
        }

        private bool EvaluateNullEqualiy(JoinPartyRequest other)
        {
            bool equalNullParties = Party == null && other.Party == null;
            if (equalNullParties)
            {
                return
                    IsSuccess.Equals(other.IsSuccess) &&
                    StatusCode.Equals(other.StatusCode);
            }
            if (Party != null && other.Party != null)
            {
                if (Party.PartyHost != null || other.Party.PartyHost != null)
                {
                    return false;
                }
                if (Party.PartyGuest != null || other.Party.PartyGuest != null)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var host = Party.PartyHost;
            var guest = Party.PartyGuest;
            var lobbyCode = Party.LobbyCode;
            return new { IsSuccess, StatusCode, host, guest, lobbyCode }.GetHashCode();
        }
    }
}
