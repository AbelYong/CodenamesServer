using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using Services.DTO;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IUserManager
    {
        [OperationContract]
        Player GetPlayerByUserID(Guid userID);
        [OperationContract]
        SignInRequest SignIn(Player svPlayer);
        [OperationContract]
        CommunicationRequest UpdateProfile(Player updatedPlayer);
    }
}
