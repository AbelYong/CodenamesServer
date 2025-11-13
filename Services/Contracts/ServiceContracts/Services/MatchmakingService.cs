using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class MatchmakingService : IMatchmakingManager
    {
        public MatchRequest GetMatchWithAFriend(MatchConfiguration configuration)
        {
            MatchRequest request = new MatchRequest();
            if (configuration != null && configuration.Requester != null)
            {
                request.IsSuccess = false;
                request.StatusCode = DTO.StatusCode.MISSING_DATA;
            }
            Match match = new Match();
            request.Match = match;
            request.Match.Requester = configuration.Requester; 
            request.Match = MatchmakingOperation.GenerateMatch(configuration);
            request.IsSuccess = true;
            request.StatusCode = DTO.StatusCode.CREATED;
            return request;
        }

        public CommunicationRequest CancelMatch()
        {
            throw new NotImplementedException();
        }
    }
}
