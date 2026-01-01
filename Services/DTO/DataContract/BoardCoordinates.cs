using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used inside the []PickedNotifications
    /// Row and Columns should be integers between 0-4
    /// </summary>
    [DataContract]
    public class BoardCoordinates
    {
        [DataMember]
        public int Row { get; set; }
        [DataMember]
        public int Column { get; set; }
    }
}
