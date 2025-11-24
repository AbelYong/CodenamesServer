using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class BoardCoordinates
    {
        [DataMember]
        public int Row { get; set; }
        [DataMember]
        public int Column { get; set; }
    }
}
