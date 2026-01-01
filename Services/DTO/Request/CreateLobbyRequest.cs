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
        public string LobbyCode { get; set; }
    }
}
