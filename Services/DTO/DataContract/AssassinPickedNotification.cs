using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class AssassinPickedNotification
    {
        [DataMember]
        public Guid SenderID { get; set; }
        [DataMember]
        public BoardCoordinates Coordinates { get; set; }
        [DataMember]
        public string FinalMatchLength { get; set; }
    }
}
