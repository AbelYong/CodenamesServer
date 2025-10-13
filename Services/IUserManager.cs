using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Services.DTO;

namespace Services
{
    [ServiceContract]
    public interface IUserManager
    {
        [OperationContract]
        Player GetPlayerByUserID(Guid userID);
    }
}
