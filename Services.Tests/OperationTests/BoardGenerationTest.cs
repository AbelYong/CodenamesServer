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
        private MatchConfiguration _matchConfig;

        [SetUp]
        public void Setup()
        {
            MatchConfiguration config = new MatchConfiguration();
            config.Requester = new DTO.Player();
            config.Requester.PlayerID = Guid.NewGuid();
            config.MatchRules = new MatchRules();
            _matchConfig = config;
        }

        [Test]
        public void GenerateMatch_NormalMatch_HasNineAgents()
        {
            _matchConfig.MatchRules.Gamemode = Gamemode.NORMAL;

        }
    }
}
