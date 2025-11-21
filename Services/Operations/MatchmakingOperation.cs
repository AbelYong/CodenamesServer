using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Operations
{
    public static class MatchmakingOperation
    {
        private static Random _random = new Random();
        private const int _MAX_ASSASSINS = 3;

        public static Match GenerateMatch(MatchConfiguration configuration)
        {
            Match match = new Match();
            match.MatchID = Guid.NewGuid();
            match.Requester = configuration.Requester;
            match.Companion = configuration.Companion;
            SetMatchRules(match, configuration);
            GenerateBoards(match);
            match.SelectedWords = SelectWordList();
            return match;
        }

        private static void SetMatchRules(Match match, MatchConfiguration configuration)
        {
            switch (configuration.MatchRules.Gamemode)
            {
                case Gamemode.NORMAL:
                    match.Rules = GetNormalRules();
                    break;
                case Gamemode.CUSTOM:
                    match.Rules = GetCustomRules(configuration);
                    break;
                case Gamemode.COUNTERINTELLIGENCE:
                    match.Rules = GetCounterIntelligenceRules();
                    break;
                default:
                    match.Rules = GetNormalRules();
                    break;
            }
        }

        private static MatchRules GetNormalRules()
        {
            const int TURN_TIMER = 30;
            const int TIMER_TOKENS = 9;
            const int BYSTANDER_TOKENS = 0;
            
            MatchRules rules = new MatchRules();
            rules.Gamemode = Gamemode.NORMAL;

            rules.TurnTimer = TURN_TIMER;
            rules.TimerTokens = TIMER_TOKENS;
            rules.BystanderTokens = BYSTANDER_TOKENS;
            rules.SetMaxAssassins(_MAX_ASSASSINS);
            
            return rules;
        }

        private static MatchRules GetCustomRules(MatchConfiguration configuration)
        {
            int maxTurnTimer = MatchRules.MAX_TURN_TIMER;
            int maxTimerTokens = MatchRules.MAX_TIMER_TOKENS;
            int maxBystanderTokens = MatchRules.MAX_BYSTANDER_TOKENS;

            MatchRules rules = new MatchRules();
            rules.Gamemode = Gamemode.CUSTOM;
            
            int turnTimer = configuration.MatchRules.TurnTimer;
            rules.TurnTimer = turnTimer < maxTurnTimer ? turnTimer : maxTurnTimer;

            int timerTokens = configuration.MatchRules.TimerTokens;
            rules.TimerTokens = timerTokens < maxTimerTokens ? timerTokens : maxTimerTokens;

            int bystanderTokens = configuration.MatchRules.BystanderTokens;
            rules.BystanderTokens = bystanderTokens < maxBystanderTokens ? bystanderTokens : maxBystanderTokens;

            rules.SetMaxAssassins(_MAX_ASSASSINS);
            
            return rules;
        }

        private static MatchRules GetCounterIntelligenceRules()
        {
            const int TURN_TIMER = 45;
            const int TIMER_TOKENS = 12;
            const int BYSTANDER_TOKENS = 0;
            const int COUNTERINTELLIGENCE_ASSASSINS = 16;

            MatchRules rules = new MatchRules();
            rules.Gamemode = Gamemode.COUNTERINTELLIGENCE;
            
            rules.TurnTimer = TURN_TIMER;
            rules.TimerTokens = TIMER_TOKENS;
            rules.BystanderTokens = BYSTANDER_TOKENS;
            rules.SetMaxAssassins(COUNTERINTELLIGENCE_ASSASSINS);

            return rules;
        }

        private static void GenerateBoards(Match match)
        {
            // Configuration for generating the board
            const int MAX_ROWS = 5;
            const int MAX_COLUMNS = 5;
            const int MATCHING_AGENTS = 3;
            int localBystanders = GetLocalBystanders(match.Rules.Gamemode);

            int[,] boardPlayerOne = new int[MAX_ROWS, MAX_COLUMNS];
            int[,] boardPlayerTwo = new int[MAX_ROWS, MAX_COLUMNS];

            int numberOfRows = boardPlayerOne.GetLength(0);
            int numberOfColumns = boardPlayerOne.GetLength(1);
            int totalSpots = numberOfRows * numberOfColumns;

            //Generating board one
            List<int> boardOnePositions = Enumerable.Range(0, totalSpots).ToList();
            Shuffle(boardOnePositions);

            SetAssassins(boardOnePositions, boardPlayerOne, match.Rules.MaxAssassins);
            SetBystanders(boardOnePositions, boardPlayerOne, match.Rules.MaxAssassins, localBystanders);

            //Generating board two, last three positions should always be agents
            List<int> sharedPositions = boardOnePositions.GetRange(totalSpots - MATCHING_AGENTS, MATCHING_AGENTS);
            List<int> boardTwoPositions = Enumerable.Range(0, totalSpots).ToList();
            boardTwoPositions.RemoveAll(x => sharedPositions.Contains(x));
            Shuffle(boardTwoPositions);

            SetAssassins(boardTwoPositions, boardPlayerTwo, match.Rules.MaxAssassins);
            SetBystanders(boardTwoPositions, boardPlayerTwo, match.Rules.MaxAssassins, localBystanders);

            match.BoardPlayerOne = ConvertToJagged(boardPlayerOne);
            match.BoardPlayerTwo = ConvertToJagged(boardPlayerTwo);
        }

        private static int GetLocalBystanders(Gamemode gameMode)
        {
            int localBystanders;
            switch (gameMode)
            {
                case Gamemode.NORMAL:
                    localBystanders = 13;
                    break;
                case Gamemode.CUSTOM:
                    localBystanders = 13;
                    break;
                case Gamemode.COUNTERINTELLIGENCE:
                    localBystanders = 0;
                    break;
                default:
                    localBystanders = 13;
                    break;
            }
            return localBystanders;
        }

        private static void Shuffle(List<int> positions)
        {
            //Shuffle the positions using Fisher-Yates algorithm
            int n = positions.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);

                int value = positions[k];
                positions[k] = positions[n];
                positions[n] = value;
            }
        }

        private static void SetAssassins(List<int> positions, int[,] board, int maxAssassins)
        {
            int numberOfColumns = board.GetLength(1);
            const int ASSASSIN_CODE = 2;
            for (int i = 0; i < maxAssassins; i++)
            {
                int flatIndex = positions[i];
                int row = flatIndex / numberOfColumns;
                int column = flatIndex % numberOfColumns;
                board[row, column] = ASSASSIN_CODE;
            }
        }

        private static void SetBystanders(List<int> positions, int[,] board, int maxAssassins, int maxBystanders)
        {
            int numberOfColumns = board.GetLength(1);
            const int BYSTANDER_CODE = 1;
            int startIndex = maxAssassins;
            int endIndex = startIndex + maxBystanders;
            for (int i = startIndex; i < endIndex; i++)
            {
                int flatIndex = positions[i];
                int row = flatIndex / numberOfColumns;
                int column = flatIndex % numberOfColumns;
                board[row, column] = BYSTANDER_CODE;
            }
        }

        private static List<int> SelectWordList()
        {
            const int NUMBER_OF_WORDS = 400;
            const int MAX_KEYWORDS = 25;
            List<int> selectedWords = Enumerable.Range(0, NUMBER_OF_WORDS).ToList();
            Shuffle(selectedWords);
            selectedWords = selectedWords.GetRange(0, MAX_KEYWORDS);
            return selectedWords;
        }

        // Converting 2D array [,] to jagged array [][] is needed for DataContract serialization
        private static int[][] ConvertToJagged(int[,] multiArray)
        {
            int rows = multiArray.GetLength(0);
            int columns = multiArray.GetLength(1);

            int[][] jaggedArray = new int[rows][];

            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new int[columns];
                for (int j = 0; j < columns; j++)
                {
                    jaggedArray[i][j] = multiArray[i, j];
                }
            }
            return jaggedArray;
        }
    }
}
