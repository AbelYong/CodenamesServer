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
    }
}
