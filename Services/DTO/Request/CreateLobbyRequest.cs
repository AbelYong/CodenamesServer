using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    [DataContract]
    public class CreateLobbyRequest : Request
    {
        [DataMember]
        public string LobbyCode { get; set; }
    }
}
