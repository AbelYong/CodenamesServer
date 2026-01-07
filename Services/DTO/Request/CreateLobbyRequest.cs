using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// Used by LobbyService to return the generated code back to the Party's Host
    /// </summary>
    [DataContract]
    public class CreateLobbyRequest : Request
    {
        [DataMember]
        public string LobbyCode { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj is CreateLobbyRequest other)
            {
                return
                    IsSuccess.Equals(other.IsSuccess) &&
                    StatusCode.Equals(other.StatusCode) &&
                    LobbyCode.Equals(other.LobbyCode);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { IsSuccess, StatusCode, LobbyCode }.GetHashCode();
        }
    }
}
