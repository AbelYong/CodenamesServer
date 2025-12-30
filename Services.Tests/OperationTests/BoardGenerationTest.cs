using NUnit.Framework;
using System;
using Services.Operations;
using Services.DTO.DataContract;

namespace Services.Tests.MatchmakingTests
{
    [TestFixture]
    public class BoardGenerationTest
    {
        private const int _BOARD_SIZE = 5;
        private MatchConfiguration _matchConfig;

        [SetUp]
        public void Setup()
        {
            MatchConfiguration config = new MatchConfiguration();
            config.MatchRules = new MatchRules();
            _matchConfig = config;
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        [TestCase(Gamemode.COUNTERINTELLIGENCE)]
        public void GenerateMatch_AllMatchTypes_HasNineAgents(Gamemode gamemode)
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            //Act
            int boardSize = 5;
            int agentCode = 0;
            int agentAmountBoardOne = 0;
            int agentAmountBoardTwo = 0;
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (boardPlayerOne[i][j] == agentCode)
                    {
                        agentAmountBoardOne++;
                        
                    }
                    if (boardPlayerTwo[i][j] == agentCode)
                    {
                        agentAmountBoardTwo++;
                    }
                }
            }


            //Assert
            int maxAgents = 9;
            Assert.That(agentAmountBoardOne.Equals(maxAgents) && agentAmountBoardTwo.Equals(maxAgents));
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        public void GenerateMatch_StandardTypes_HaveThirteenBystanders(Gamemode gamemode)
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            //Act
            int bystanderAmountBoardOne = 0;
            int bystanderAmountBoardTwo = 0;
            CountBystanders(boardPlayerOne, ref bystanderAmountBoardOne, boardPlayerTwo, ref bystanderAmountBoardTwo);

            //Assert
            int maxBystanders = 13;
            Assert.That(bystanderAmountBoardOne.Equals(maxBystanders) && bystanderAmountBoardTwo.Equals(maxBystanders));
        }

        [Test]
        public void GenerateMatch_Counterintelligence_HasZeroBystanders()
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            //Act
            int bystanderAmountBoardOne = 0;
            int bystanderAmountBoardTwo = 0;
            CountBystanders(boardPlayerOne, ref bystanderAmountBoardOne, boardPlayerTwo, ref bystanderAmountBoardTwo);

            //Assert
            int maxBystanders = 0;
            Assert.That(bystanderAmountBoardOne.Equals(maxBystanders) && bystanderAmountBoardTwo.Equals(maxBystanders));
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        public void GenerateMatch_StandardTypes_HaveThreeAssassins(Gamemode gamemode)
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            //Act
            int assassinAmountBoardOne = 0;
            int assassinAmountBoardTwo = 0;
            CountAssassins(boardPlayerOne, ref assassinAmountBoardOne, boardPlayerTwo, ref assassinAmountBoardTwo);

            //Assert
            int maxAssassins = 3;
            Assert.That(assassinAmountBoardOne.Equals(maxAssassins) && assassinAmountBoardTwo.Equals(maxAssassins));
        }

        [Test]
        public void GenerateMatch_Counterintelligence_HasSixteenAssassins()
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            //Act
            int assassinAmountBoardOne = 0;
            int assassinAmountBoardTwo = 0;
            CountAssassins(boardPlayerOne, ref assassinAmountBoardOne, boardPlayerTwo, ref assassinAmountBoardTwo);

            //Assert
            int maxAssassins = 16;
            Assert.That(assassinAmountBoardOne.Equals(maxAssassins) && assassinAmountBoardTwo.Equals(maxAssassins));
        }

        [Test]
        public void GenerateMatch_Normal_FollowsStandardRules()
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = Gamemode.NORMAL;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);

            //Assert
            Assert.That(
                newMatch.Rules.TurnTimer.Equals(MatchRules.NORMAL_TURN_TIMER) &&
                newMatch.Rules.TimerTokens.Equals(MatchRules.NORMAL_TIMER_TOKENS) &&
                newMatch.Rules.BystanderTokens.Equals(MatchRules.NORMAL_BYSTANDER_TOKENS) &&
                newMatch.Rules.MaxAssassins.Equals(MatchRules.NORMAL_MAX_ASSASSINS));
        }

        [Test]
        public void GenerateMatch_Custom_FollowsUpperBoundary()
        {
            //Arrange
            _matchConfig.MatchRules = new MatchRules
            {
                Gamemode = Gamemode.CUSTOM,
                TurnTimer = 999,
                TimerTokens = 999,
                BystanderTokens = 999,
                MaxAssassins = 999, //Note: Max assassins is not set by the client, this verifies the client cannot customize this value

            };
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);

            //Assert
            Assert.That(
                newMatch.Rules.TurnTimer.Equals(MatchRules.MAX_TURN_TIMER) &&
                newMatch.Rules.TimerTokens.Equals(MatchRules.MAX_TIMER_TOKENS) &&
                newMatch.Rules.BystanderTokens.Equals(MatchRules.MAX_BYSTANDER_TOKENS) &&
                newMatch.Rules.MaxAssassins.Equals(MatchRules.NORMAL_MAX_ASSASSINS)
                );
        }

        [Test]
        public void GenerateMatchCustom_FollowsPlayerSelection()
        {
            //Arrange
            int customTurnTimer = 50;
            int customTimerTokens = 12;
            int customBystanderTokens = 4;
            _matchConfig.MatchRules = new MatchRules
            {
                Gamemode = Gamemode.CUSTOM,
                TurnTimer = customTurnTimer,
                TimerTokens = customTimerTokens,
                BystanderTokens = customBystanderTokens,
            };
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);

            //Assert
            Assert.That(
                newMatch.Rules.TurnTimer.Equals(customTurnTimer) &&
                newMatch.Rules.TimerTokens.Equals(customTimerTokens) &&
                newMatch.Rules.BystanderTokens.Equals(customBystanderTokens) &&
                newMatch.Rules.MaxAssassins.Equals(MatchRules.NORMAL_MAX_ASSASSINS)
                );
        }

        [Test]
        public void GenerateMatch_Counterintelligence_FollowsCounterintellegenceRules()
        {
            //Arrange
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);

            //Assert
            Assert.That(
                newMatch.Rules.TurnTimer.Equals(MatchRules.COUNTERINT_TURN_TIMER) &&
                newMatch.Rules.TimerTokens.Equals(MatchRules.COUNTERINT_TIMER_TOKENS) &&
                newMatch.Rules.BystanderTokens.Equals(MatchRules.COUNTERINT_BYSTANDER_TOKENS) &&
                newMatch.Rules.MaxAssassins.Equals(MatchRules.COUNTERINT_ASSASSINS));
        }

        private static void CountBystanders(int[][] boardPlayerOne, ref int amountBoardOne, int[][] boardPlayerTwo, ref int amountBoardTwo)
        {
            int bystanderCode = 1;
            for (int i = 0; i < _BOARD_SIZE; i++)
            {
                for (int j = 0; j < _BOARD_SIZE; j++)
                {
                    if (boardPlayerOne[i][j] == bystanderCode)
                    {
                        amountBoardOne++;

                    }
                    if (boardPlayerTwo[i][j] == bystanderCode)
                    {
                        amountBoardTwo++;
                    }
                }
            }
        }

        private static void CountAssassins(int[][] boardPlayerOne, ref int amountBoardOne, int[][] boardPlayerTwo, ref int amountBoardTwo)
        {
            int assassinCode = 2;
            for (int i = 0; i < _BOARD_SIZE; i++)
            {
                for (int j = 0; j < _BOARD_SIZE; j++)
                {
                    if (boardPlayerOne[i][j] == assassinCode)
                    {
                        amountBoardOne++;

                    }
                    if (boardPlayerTwo[i][j] == assassinCode)
                    {
                        amountBoardTwo++;
                    }
                }
            }
        }
    }
}
