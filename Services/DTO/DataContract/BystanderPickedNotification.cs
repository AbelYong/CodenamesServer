using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class BystanderPickedNotification
    {
        [DataMember]
        public Guid SenderID {  get; set; }
        [DataMember]
        public BoardCoordinates Coordinates { get; set; }
        [DataMember]
        public TokenType TokenToUpdate { get; set; }
        [DataMember]
        public int RemainingTokens { get; set; }
    }
}
