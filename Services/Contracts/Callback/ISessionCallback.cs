using Services.DTO;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Services.Contracts
{
    [ServiceContract]
    public interface ISessionCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyFriendOnline(Player player);

        [OperationContract(IsOneWay = true)]
        void NotifyFriendOffline(Guid playerId);

        [OperationContract(IsOneWay = true)]
        void ReceiveOnlineFriends(List<Player> friends);

        [OperationContract(IsOneWay = true)]
        void NotifyKicked(KickReason reason);
    }
}
