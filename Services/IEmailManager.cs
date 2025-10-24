using Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Services
{
    [ServiceContract]
    public interface IEmailManager
    {
        [OperationContract]
        RequestResult SendVerificationCode(string email);

        [OperationContract]
        RequestResult ValidateVerificationCode(string email, string code);
    }
}
