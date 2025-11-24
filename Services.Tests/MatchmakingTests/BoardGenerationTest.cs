using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Operations;
using Services.DTO.DataContract;

namespace Services.Tests.MatchmakingTests
{
    [TestFixture]
    public class BoardGenerationTest
    {
        [Test]
        public void GenerateMatch_Requester_MatchesConfiguration()
        {
            MatchConfiguration config = new MatchConfiguration();
            config.Requester = new DTO.Player();
            config.Requester.PlayerID = Guid.NewGuid();
            Guid requesterID = (Guid)config.Requester.PlayerID;
        }
    }
}
