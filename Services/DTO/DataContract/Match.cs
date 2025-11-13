using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
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
    }
}
