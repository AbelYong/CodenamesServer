using System;
using System.ServiceModel;
using Services.DTO.DataContract;
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
