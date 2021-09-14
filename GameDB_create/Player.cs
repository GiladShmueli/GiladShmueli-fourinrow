using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace GameDB_create
{
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public IEnumerable<Game> BlueGames { get; set; }
        public IEnumerable<Game> RedGames { get; set; }
        public IEnumerable<Game> Victories { get; set; }
        public int Score { get; set; }
    }
}
