using System;
using System.Collections;
using System.Collections.Generic;

namespace GameDB_create
{
    public class Game
    {
        public int GameId { get; set; }
        public Player BluePlayer { get; set; } //Blue starts game
        public int BlueScore { get; set; }
        public Player RedPlayer { get; set; }
        public int RedScore { get; set; }
        public DateTime EndTime { get; set; }
        public Player Winner { get; set; }
        public int SquaresMarked { get; set; }
    }
}