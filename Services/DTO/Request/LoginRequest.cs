using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO.Request
{
    [DataContract]
    public class LoginRequest : Request
    {
        [DataMember]
        public Guid? UserID { get; set; }
        public LoginRequest()
        {
            
        }
    }
}
