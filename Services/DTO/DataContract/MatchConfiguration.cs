using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO.DataContract
{
    [DataContract]
    public class MatchConfiguration
    {
        [DataMember]
        public Player Requester { get; set; }

        [DataMember]
        public Player Companion { get; set; }

        [DataMember]
        public MatchRules MatchRules { get; set; }
    }
}
