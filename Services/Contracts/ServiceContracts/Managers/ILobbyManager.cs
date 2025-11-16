using Services.Contracts.Callback;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract(CallbackContract = typeof(ILobbyCallback), SessionMode = SessionMode.Required)]
    public interface ILobbyManager
    {
        [OperationContract(IsOneWay = false)]
        CommunicationRequest Connect(Guid playerID);

        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid playerID);

        [OperationContract(IsOneWay = false)]
        CreateLobbyRequest CreateParty(Guid playerID);

        [OperationContract(IsOneWay = false)]
        CommunicationRequest InviteToParty(Guid hostPlayerID, Guid friendToInviteID, string lobbyCode);

        [OperationContract(IsOneWay = false)]
        JoinPartyRequest JoinParty(Guid joiningPlayerID, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void LeaveParty(Guid playerID, string lobbyCode);
    }
}
