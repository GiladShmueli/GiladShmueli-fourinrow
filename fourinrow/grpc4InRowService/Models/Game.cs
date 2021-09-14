using System;
using System.Collections.Generic;

#nullable disable

namespace grpc4InRowService.Models
{
    public partial class Game
    {
        public int GameId { get; set; }
        public int? BluePlayerUserId { get; set; }
        public int BlueScore { get; set; }
        public int? RedPlayerUserId { get; set; }
        public int RedScore { get; set; }
        public DateTime EndTime { get; set; }
        public int? WinnerUserId { get; set; }
        public int SquaresMarked { get; set; }

        public virtual Player BluePlayerUser { get; set; }
        public virtual Player RedPlayerUser { get; set; }
        public virtual Player WinnerUser { get; set; }
    }
}
