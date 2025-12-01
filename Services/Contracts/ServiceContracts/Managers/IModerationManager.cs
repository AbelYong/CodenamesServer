using Services.DTO;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IModerationManager
    {
        /// <summary>
        /// Receives a report against a player and applies penalties if thresholds are met.
        /// </summary>
        /// <param name="reportedUserID">The ID of the user being reported.</param>
        /// <param name="reason">The reason for the report.</param>
        /// <returns>A RequestResult indicating the outcome (Success, Duplicate, Kicked, etc.).</returns>
        [OperationContract]
        CommunicationRequest ReportPlayer(Guid reporterPlayerID, Guid reportedPlayerID, string reason);
    }
}