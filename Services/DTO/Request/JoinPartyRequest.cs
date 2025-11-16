using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO.Request
{
    [DataContract]
    public class JoinPartyRequest : Request
    {
        [DataMember]
        public Party Party { get; set; }
    }
}
