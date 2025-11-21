using Services.DTO.DataContract;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    [ServiceContract]
    public interface IMatchCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyClueReceived(string clue);

        [OperationContract(IsOneWay = true)]
        void NotifyTurnChange();

        [OperationContract(IsOneWay = true)]
        void NotifyRolesChanged();

        [OperationContract(IsOneWay = true)]
        void NotifyAgentPicked(int newTurnLength);

        [OperationContract(IsOneWay = true)]
        void NotifyBystanderPicked(TokenType tokenToUpdate, int remainingTokens);

        [OperationContract(IsOneWay = true)]
        void NotifyAssassinPicked(string finalMatchLength);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchWon(string finalMatchLength);

        [OperationContract(IsOneWay = true)]
        void NotifyMatchTimeout(string finalMatchLength);

        [OperationContract(IsOneWay = true)]
        void NotifyCompanionDisconnect();

        [OperationContract(IsOneWay = true)]
        void NotifyStatsCouldNotBeSaved();
    }
}
