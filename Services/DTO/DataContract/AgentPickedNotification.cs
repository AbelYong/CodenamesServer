using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class AgentPickedNotification
    {
        [DataMember]
        public Guid SenderID { get; set; }

        [DataMember]
        public BoardCoordinates Coordinates { get; set; }

        [DataMember]
        public int NewTurnLength { get; set; }
    }
}
