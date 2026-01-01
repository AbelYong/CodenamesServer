using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by Guessers to inform they have selected a bystander
    /// SenderID and BoardCoordinates must be provided by the client.
    /// TokenToUpdate and RemainingTokens are determined by the server,
    /// the Spymaster must update the specified Token to RemaningTokens
    /// </summary>
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
