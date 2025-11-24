using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
