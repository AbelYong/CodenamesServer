using System;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by Guessers to relay the coordinates of selected agents.
    /// SenderID and BoardCoordinates must be provided by the client.
    /// NewTurnLength must be provided by the client as a number equal or lesser than 60
    /// Spymasters receive this class on their callback interface, where they can use
    /// BoardCoordinates and NewTurnLength to adjust their clients accordingly
    /// </summary>
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
