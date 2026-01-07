using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Defines the players, rules and configuration (keycards, wordlist) to be used on a Match
    /// Requester and Companion (and their playerIDs) are required
    /// MatchRules are requiered by MatchmakingService.
    /// The Board of player one is the Requester's board, and the companion's keycard
    /// The Board of playerTwo is the Companion's board, and the requester's keycard
    /// SelectedWords is a list of random integers in the range of 0-400
    /// </summary>
    [DataContract]
    public class Match
    {
        const int BOARD_SIZE = 5;

        [DataMember]
        public Guid MatchID { get; set; }

        [DataMember]
        public Player Requester { get; set; }

        [DataMember]
        public Player Companion { get; set; }

        [DataMember]
        public MatchRules Rules { get; set; }

        [DataMember]
        public int[][] BoardPlayerOne { get; set; }

        [DataMember]
        public int[][] BoardPlayerTwo { get; set; }

        [DataMember]
        public List<int> SelectedWords { get; set; }

        public Match()
        {
            Requester = new Player();
            Companion = new Player();
            Rules = new MatchRules();
            BoardPlayerOne = new int[BOARD_SIZE][];
            BoardPlayerTwo = new int[BOARD_SIZE][];
            SelectedWords= new List<int>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Match other)
            {
                return
                    MatchID.Equals(other.MatchID) &&
                    Requester.Equals(other.Requester) &&
                    Companion.Equals(other.Companion) &&
                    Rules.Equals(other.Rules) &&
                    BoardPlayerOne.Equals(other.BoardPlayerOne) &&
                    BoardPlayerTwo.Equals(other.BoardPlayerTwo) &&
                    SelectedWords.Equals(other.SelectedWords);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new
            {
                Requester, Companion, Rules, BoardPlayerOne, BoardPlayerTwo, SelectedWords
            }.GetHashCode();
        }
    }
}
