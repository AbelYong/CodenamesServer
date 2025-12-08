using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO.Request
{
    [DataContract]
    public class PasswordResetRequest : Request
    {
        [DataMember]
        public int RemainingAttempts { get; set; }
    }
}
