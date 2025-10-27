using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using Services.DTO;

namespace Services
{
    [ServiceContract]
    public interface IUserManager
    {
        [OperationContract]
        Player GetPlayerByUserID(Guid userID);

        [OperationContract]
        UpdateResult UpdateProfile(Player updatedPlayer);
    }

    [DataContract]
    public class UpdateResult
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
