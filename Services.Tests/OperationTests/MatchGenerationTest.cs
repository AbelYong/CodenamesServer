using NUnit.Framework;
using Services.Operations;
using Services.DTO.DataContract;
using System.Linq;

namespace Services.Tests.MatchmakingTests
{
    [TestFixture]
    public class MatchGenerationTest
    {
        private const int _BOARD_SIZE = 5;
        private MatchConfiguration _matchConfig;

        [SetUp]
        public void Setup()
        {
            MatchConfiguration config = new MatchConfiguration();
            config.MatchRules = new MatchRules();
            config.Requester = new Player();
            config.Companion = new Player();
            _matchConfig = config;
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        [TestCase(Gamemode.COUNTERINTELLIGENCE)]
        public void GenerateMatch_AllMatchTypes_HaveNineAgents(Gamemode gamemode)
        {
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

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
            
            int maxAgents = 9;
            Assert.That(agentAmountBoardOne.Equals(maxAgents) && agentAmountBoardTwo.Equals(maxAgents));
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        public void GenerateMatch_StandardTypes_HaveThirteenBystanders(Gamemode gamemode)
        {
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            int bystanderAmountBoardOne = 0;
            int bystanderAmountBoardTwo = 0;
            CountBystanders(boardPlayerOne, ref bystanderAmountBoardOne, boardPlayerTwo, ref bystanderAmountBoardTwo);

            int maxBystanders = 13;
            Assert.That(bystanderAmountBoardOne.Equals(maxBystanders) && bystanderAmountBoardTwo.Equals(maxBystanders));
        }

        [Test]
        public void GenerateMatch_Counterintelligence_HasZeroBystanders()
        {
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            int bystanderAmountBoardOne = 0;
            int bystanderAmountBoardTwo = 0;
            CountBystanders(boardPlayerOne, ref bystanderAmountBoardOne, boardPlayerTwo, ref bystanderAmountBoardTwo);

            int maxBystanders = 0;
            Assert.That(bystanderAmountBoardOne.Equals(maxBystanders) && bystanderAmountBoardTwo.Equals(maxBystanders));
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        public void GenerateMatch_StandardTypes_HaveThreeAssassins(Gamemode gamemode)
        {
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            int assassinAmountBoardOne = 0;
            int assassinAmountBoardTwo = 0;
            CountAssassins(boardPlayerOne, ref assassinAmountBoardOne, boardPlayerTwo, ref assassinAmountBoardTwo);

            int maxAssassins = 3;
            Assert.That(assassinAmountBoardOne.Equals(maxAssassins) && assassinAmountBoardTwo.Equals(maxAssassins));
        }

        [Test]
        public void GenerateMatch_Counterintelligence_HasSixteenAssassins()
        {
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            int[][] boardPlayerOne = newMatch.BoardPlayerOne;
            int[][] boardPlayerTwo = newMatch.BoardPlayerTwo;

            int assassinAmountBoardOne = 0;
            int assassinAmountBoardTwo = 0;
            CountAssassins(boardPlayerOne, ref assassinAmountBoardOne, boardPlayerTwo, ref assassinAmountBoardTwo);

            int maxAssassins = 16;
            Assert.That(assassinAmountBoardOne.Equals(maxAssassins) && assassinAmountBoardTwo.Equals(maxAssassins));
        }

        [Test]
        public void GenerateMatch_Normal_FollowsStandardRules()
        {
            _matchConfig.MatchRules.Gamemode = Gamemode.NORMAL;
            Match expected = new Match
            {
                Requester = _matchConfig.Requester,
                Companion = _matchConfig.Companion,
                Rules = new MatchRules
                {
                    Gamemode = Gamemode.NORMAL,
                    TurnTimer = MatchRules.NORMAL_TURN_TIMER,
                    TimerTokens = MatchRules.NORMAL_TIMER_TOKENS,
                    BystanderTokens = MatchRules.NORMAL_BYSTANDER_TOKENS,
                    MaxAssassins = MatchRules.NORMAL_MAX_ASSASSINS
                }
            };
            
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            CopyGeneratedParameters(newMatch, expected);

            Assert.That(newMatch.Equals(expected));
        }

        [Test]
        public void GenerateMatch_Custom_FollowsUpperBoundary()
        {
            _matchConfig.MatchRules = new MatchRules
            {
                Gamemode = Gamemode.CUSTOM,
                TurnTimer = 999,
                TimerTokens = 999,
                BystanderTokens = 999,
                MaxAssassins = 999,
            };
            Match expected = new Match
            {
                Requester = _matchConfig.Requester,
                Companion = _matchConfig.Companion,
                Rules = new MatchRules
                {
                    Gamemode = Gamemode.CUSTOM,
                    TurnTimer = MatchRules.MAX_TURN_TIMER,
                    TimerTokens = MatchRules.MAX_TIMER_TOKENS,
                    BystanderTokens = MatchRules.MAX_BYSTANDER_TOKENS,
                    MaxAssassins = MatchRules.NORMAL_MAX_ASSASSINS
                }
            };

            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            CopyGeneratedParameters(newMatch, expected);

            Assert.That(newMatch.Equals(expected));
        }

        [Test]
        public void GenerateMatchCustom_FollowsPlayerSelection()
        {
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
            Match expected = new Match
            {
                Requester = _matchConfig.Requester,
                Companion = _matchConfig.Companion,
                Rules = new MatchRules
                {
                    Gamemode = Gamemode.CUSTOM,
                    TurnTimer = customTurnTimer,
                    TimerTokens = customTimerTokens,
                    BystanderTokens = customBystanderTokens,
                    MaxAssassins = MatchRules.NORMAL_MAX_ASSASSINS
                }
            };

            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            CopyGeneratedParameters(newMatch, expected);

            Assert.That(newMatch.Equals(expected));
        }

        [Test]
        public void GenerateMatch_Counterintelligence_FollowsCounterintellegenceRules()
        {
            _matchConfig.MatchRules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            Match expected = new Match
            {
                Requester = _matchConfig.Requester,
                Companion = _matchConfig.Companion,
                Rules = new MatchRules
                {
                    Gamemode = Gamemode.COUNTERINTELLIGENCE,
                    TurnTimer = MatchRules.COUNTERINT_TURN_TIMER,
                    TimerTokens = MatchRules.COUNTERINT_TIMER_TOKENS,
                    BystanderTokens = MatchRules.COUNTERINT_BYSTANDER_TOKENS,
                    MaxAssassins = MatchRules.COUNTERINT_ASSASSINS
                }
            };

            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);
            CopyGeneratedParameters(newMatch, expected);

            Assert.That(newMatch.Equals(expected));
        }

        [TestCase(Gamemode.NORMAL)]
        [TestCase(Gamemode.CUSTOM)]
        [TestCase(Gamemode.COUNTERINTELLIGENCE)]
        public void GenerateMatch_AllModes_HaveTwentyFiveWords(Gamemode gamemode)
        {
            _matchConfig.MatchRules.Gamemode = gamemode;
            Match newMatch = MatchmakingOperation.GenerateMatch(_matchConfig);

            int wordlistSize = 25;
            Assert.That(newMatch.SelectedWords.Count, Is.EqualTo(wordlistSize));
        }

        private static void CopyGeneratedParameters(Match generated, Match copy)
        {
            copy.MatchID = generated.MatchID;
            copy.BoardPlayerTwo = generated.BoardPlayerTwo;
            copy.BoardPlayerOne = generated.BoardPlayerOne;
            copy.SelectedWords = generated.SelectedWords;
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
