using Services.DTO.DataContract;
using System;
using System.Collections.Generic;
using System.Linq;

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

            int[,] boardPlayerOne = new int[MAX_ROWS, MAX_COLUMNS];
            int[,] boardPlayerTwo = new int[MAX_ROWS, MAX_COLUMNS];

            int numberOfRows = boardPlayerOne.GetLength(0);
            int numberOfColumns = boardPlayerOne.GetLength(1);
            int totalSpots = numberOfRows * numberOfColumns;

            List<int> keycardPlayerOne = Enumerable.Repeat(0, totalSpots).ToList();
            List<int> keycardPlayerTwo = Enumerable.Repeat(0, totalSpots).ToList();

            if (match.Rules.Gamemode != Gamemode.COUNTERINTELLIGENCE)
            {
                SetBystandersOnKeycard(keycardPlayerOne, KeycardNumber.KEYCARD_ONE);
                SetBystandersOnKeycard(keycardPlayerTwo, KeycardNumber.KEYCARD_TWO);
                SetAssassinsOnKeycard(keycardPlayerOne, KeycardNumber.KEYCARD_ONE);
                SetAssassinsOnKeycard(keycardPlayerTwo, KeycardNumber.KEYCARD_TWO);
            }
            else
            {
                SetCounterintelligenceKeycard(keycardPlayerOne, KeycardNumber.KEYCARD_ONE);
                SetCounterintelligenceKeycard(keycardPlayerTwo, KeycardNumber.KEYCARD_TWO);
            }

            List<int> boardPositions = Enumerable.Range(0, totalSpots).ToList();
            Shuffle(boardPositions);
            SetBoard(boardPlayerOne, boardPositions, keycardPlayerOne, totalSpots);
            SetBoard(boardPlayerTwo, boardPositions, keycardPlayerTwo, totalSpots);

            match.BoardPlayerOne = ConvertToJagged(boardPlayerOne);
            match.BoardPlayerTwo = ConvertToJagged(boardPlayerTwo);
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

        private static void SetBystandersOnKeycard(List<int> keycard, KeycardNumber keycardNum)
        {
            const int BYSTANDER_CODE = 1;
            const int KEYCARD_ONE_START = 1;
            const int KEYCARD_TWO_START = 9;
            int startPosition = keycardNum == KeycardNumber.KEYCARD_ONE ? KEYCARD_ONE_START : KEYCARD_TWO_START;
            const int KEYCARD_ONE_END = 5;
            const int KEYCARD_TWO_END = 13;
            int endPosition = keycardNum == KeycardNumber.KEYCARD_ONE ? KEYCARD_ONE_END : KEYCARD_TWO_END;
            
            for (int i = startPosition; i <=  endPosition; i++)
            {
                keycard[i] = BYSTANDER_CODE;
            }

            if (keycardNum == KeycardNumber.KEYCARD_ONE)
            {
                const int ODD_ONE_POSITION = 15;
                keycard[ODD_ONE_POSITION] = BYSTANDER_CODE;
            }

            const int SHARED_SECOND_START = 17;
            startPosition = SHARED_SECOND_START;
            const int KEYCARD_ONE_SECOND_END = 23;
            const int KEYCARD_TWO_SECOND_END = 24;
            endPosition = keycardNum == KeycardNumber.KEYCARD_ONE ? KEYCARD_ONE_SECOND_END: KEYCARD_TWO_SECOND_END;
            
            for (int i = startPosition; i <= endPosition; i++)
            {
                keycard[i] = BYSTANDER_CODE;
            }
        }

        private static void SetAssassinsOnKeycard(List<int> keycard, KeycardNumber keycardNum)
        {
            const int ASSASSIN_CODE = 2;

            switch (keycardNum)
            {
                case KeycardNumber.KEYCARD_ONE:
                    const int KEYCARD_ONE_FIRST_ASSASSIN = 0;
                    keycard[KEYCARD_ONE_FIRST_ASSASSIN] = ASSASSIN_CODE;

                    const int KEYCARD_ONE_SECOND_ASSASSIN = 16;
                    keycard[KEYCARD_ONE_SECOND_ASSASSIN] = ASSASSIN_CODE;
                    
                    const int KEYCARD_ONE_THIRD_ASSASSIN = 24;
                    keycard[KEYCARD_ONE_THIRD_ASSASSIN] = ASSASSIN_CODE;
                    break;
                case KeycardNumber.KEYCARD_TWO:
                    const int KEYCARD_TWO_FIRST_ASSASSIN = 14;
                    keycard[KEYCARD_TWO_FIRST_ASSASSIN] = ASSASSIN_CODE;
                    
                    const int KEYCARD_TWO_SECOND_ASSASSIN = 15;
                    keycard[KEYCARD_TWO_SECOND_ASSASSIN] = ASSASSIN_CODE;

                    const int KEYCARD_TWO_THIRD_ASSASSIN = 16;
                    keycard[KEYCARD_TWO_THIRD_ASSASSIN] = ASSASSIN_CODE;
                    break;
            }
        }

        private static void SetCounterintelligenceKeycard(List<int> keycard, KeycardNumber keycardNum)
        {
            const int ASSASSIN_CODE = 2;
            const int KEYCARD_ONE_START = 1;
            const int KEYCARD_TWO_START = 9;
            int startPosition = keycardNum == KeycardNumber.KEYCARD_ONE ? KEYCARD_ONE_START : KEYCARD_TWO_START;
            const int KEYCARD_ONE_END = 5;
            const int KEYCARD_TWO_END = 24;
            int endPosition = keycardNum == KeycardNumber.KEYCARD_ONE ? KEYCARD_ONE_END : KEYCARD_TWO_END;
            
            for (int i = startPosition; i <= endPosition; i++)
            {
                keycard[i] = ASSASSIN_CODE;
            }

            if (keycardNum == KeycardNumber.KEYCARD_ONE)
            {
                const int KEYCARD_ONE_SECOND_START = 15;
                const int KEYCARD_ONE_SECOND_END = 24;
                startPosition = KEYCARD_ONE_SECOND_START;
                endPosition = KEYCARD_ONE_SECOND_END;
                for (int i = startPosition; i <= endPosition; i++)
                {
                    keycard[i] = ASSASSIN_CODE;
                }
            }
        }

        private static void SetBoard(int[,] board, List<int> positions, List<int> keycard, int totalSpots)
        {
            int numberOfColumns = board.GetLength(1);

            for (int i = 0; i < totalSpots; i++)
            {
                int flatIndex = positions[i];
                int row = flatIndex / numberOfColumns;
                int column = flatIndex % numberOfColumns;
                board[row, column] = keycard[i];
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

        private enum KeycardNumber
        {
            KEYCARD_ONE,
            KEYCARD_TWO
        }
    }
}
