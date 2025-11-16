using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class Party
    {
        [DataMember]
        public string LobbyCode { get; set; }

        [DataMember]
        public Player PartyHost { get; set; }
        
        [DataMember]
        public Player PartyGuest { get; set; }
        
        public Party(Player partyHost, string lobbyCode)
        {
            PartyHost = partyHost;
            LobbyCode = lobbyCode;
        }
    }
}
