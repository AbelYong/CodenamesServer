using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by Guessers to inform they have selected an assassin
    /// SenderID and BoardCoordinates must be provided by the client.
    /// FinalMatchLength is calculated by the Server, and received
    /// by the spymaster for displaying final match length
    /// </summary>
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
