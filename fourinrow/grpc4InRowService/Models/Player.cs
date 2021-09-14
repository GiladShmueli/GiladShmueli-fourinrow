using System;
using System.Collections.Generic;

#nullable disable

namespace grpc4InRowService.Models
{
    public partial class Player
    {
        public Player()
        {
            GameBluePlayerUsers = new HashSet<Game>();
            GameRedPlayerUsers = new HashSet<Game>();
            GameWinnerUsers = new HashSet<Game>();
        }

        public int UserId { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int Score { get; set; }

        public virtual ICollection<Game> GameBluePlayerUsers { get; set; }
        public virtual ICollection<Game> GameRedPlayerUsers { get; set; }
        public virtual ICollection<Game> GameWinnerUsers { get; set; }
    }
}
