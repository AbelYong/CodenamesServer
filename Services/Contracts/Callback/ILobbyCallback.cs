using Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts.Callback
{
    [ServiceContract]
    public interface ILobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationReceived(Player fromPlayer, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchInvitationAccepted(Player byPlayer);

        [OperationContract(IsOneWay = true)]
        void NotifyPartyAbandoned(Guid leavingPlayerID);
    }
}
