using Grpc.Core;
using grpc4InRowService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace grpc4InRowService
{
    public class GameService : Game.GameBase
    {
        private readonly ILogger<GameService> _logger;
        public GameService(ILogger<GameService> logger)
        {
            _logger = logger;
        }
        //users
        private static ConcurrentDictionary<string, List<GameMessage>> users =
            new ConcurrentDictionary<string, List<GameMessage>>();
        //games
        private static ConcurrentDictionary<GamePlayers, int> currentGames =
            new ConcurrentDictionary<GamePlayers, int>();

        private static readonly TimeSpan interval = TimeSpan.FromSeconds(2);
        public override async Task Connect(UserInfo user,
            IServerStreamWriter<GameMessage> responseStream,
            ServerCallContext context)
        {
            users.TryAdd(user.UserName, new List<GameMessage>());
            var token = context.CancellationToken;
            InformAllUsers(new GameMessage
            {
                Type = MessageType.Update,
                FromUser = "",
                ToUser = "",
                Message = "",
                Score = 0
            });
            while (!token.IsCancellationRequested)
            {
                if (users[user.UserName].Count > 0)
                {
                    foreach (var item in users[user.UserName])
                    {
                        await responseStream.WriteAsync(item);
                    }
                }
                users[user.UserName].Clear();
                await Task.Delay(interval, token);
            }
        }

        public override async Task<Empty> UserInDB(UserInfo user,
            ServerCallContext context)
        {
            string userName = user.UserName;
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                var query = from p in ctx.Players
                            where p.Name == userName
                            select p;
                //var answer = new Bool { Value = query.Count() > 0 ? Truth.True : Truth.False };
                if (query.Count() == 0)
                {
                    throw new RpcException(new Status(StatusCode.NotFound,
                    "user does not exists"));
                }
                if (query.First().Password != EncryptPW(user.Password))
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated,
                    "password does not fit the username"));
                }
                return await Task.FromResult(new Empty());
            }
        }

        public override async Task<Games> PreviousGames(Empty request, ServerCallContext context)
        {
            Games history = new Games();
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                var query = (from g in ctx.Games
                             orderby g.EndTime
                             select new GamePlayers
                             {
                                 Blue = g.BluePlayerUser.Name,
                                 Red = g.RedPlayerUser.Name,
                                 BlueScore = g.BlueScore,
                                 RedScore = g.RedScore,
                                 StartTime = g.EndTime.Day.ToString() + "/" + g.EndTime.Month.ToString() + "/" + g.EndTime.Year.ToString() + " "
                                             + g.EndTime.Hour.ToString() + ":" + g.EndTime.Minute.ToString() + ":" + g.EndTime.Second.ToString(),
                                 Turns = g.SquaresMarked
                             });
                foreach (var item in query)
                { 
                    history.GamesData.Add(item);
                }   
            }
            return await Task.FromResult(history);
        }

        public override async Task<Games> Get2PlayerHistory(Players players, ServerCallContext context)
        {
            Games history = new Games();
            PlayerModel player1 = players.Players_.ElementAt(0);
            PlayerModel player2 = players.Players_.ElementAt(1);
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                var query = (from g in ctx.Games
                             where (g.BluePlayerUser.Name == player1.Name && g.RedPlayerUser.Name == player2.Name) ||
                                   (g.BluePlayerUser.Name == player2.Name && g.RedPlayerUser.Name == player1.Name)
                             orderby g.EndTime
                             select new GamePlayers
                             {
                                 Blue = g.BluePlayerUser.Name,
                                 Red = g.RedPlayerUser.Name,
                                 BlueScore = g.BlueScore,
                                 RedScore = g.RedScore,
                                 StartTime = g.EndTime.Day.ToString() + "/" + g.EndTime.Month.ToString() + "/" + g.EndTime.Year.ToString() + " "
                                             + g.EndTime.Hour.ToString() + ":" + g.EndTime.Minute.ToString() + ":" + g.EndTime.Second.ToString(),
                                 Turns = g.SquaresMarked
                             });
                if (query.Count() <= 0)
                {
                    return await Task.FromResult(history);
                }
                foreach (var item in query)
                {
                    history.GamesData.Add(item);
                }
            }
            return await Task.FromResult(history);
        }
        private static void InformAllUsers(GameMessage gameMessage)
        {
            foreach (var item in users.Keys)
            {
                users[item].Add(gameMessage);
            }
        }

        public override async Task<Empty> Disconnect(UserInfo user,
            ServerCallContext context)
        {
            var val = new List<GameMessage>();
            int valgame;
            users.TryRemove(user.UserName, out val);
            foreach(var item in currentGames.Keys)
            {
                GamePlayers game = item as GamePlayers;
                if(game.Blue == user.UserName || game.Red == user.UserName)
                {
                    currentGames.TryRemove(game, out valgame);
                }
            }
            InformAllUsers(new GameMessage
            {
                Type = MessageType.Update,
                FromUser = "",
                ToUser = "",
                Message = "",
                Score = 0
            });
            return await Task.FromResult(new Empty());
        }

        public override async Task<Empty> SendMessage(GameMessage message,
            ServerCallContext context)
        {
            if (message.Type == MessageType.Update)
            {
                InformAllUsers(message);
            }
            else if (users.ContainsKey(message.ToUser))
            {
                users[message.ToUser].Add(message);
                if (message.Type == MessageType.Invite || message.Type == MessageType.Answer)
                    InformAllUsers(new GameMessage
                    {
                        Type = MessageType.Update,
                        FromUser = "",
                        ToUser = "",
                        Message = ""
                    });
            }
            else
            {
                throw new RpcException(
                    new Status(StatusCode.NotFound, "User is not found"));
            }
            return await Task.FromResult(new Empty());
        }

        public override async Task<Empty> UserConnected(UserInfo user,
            ServerCallContext context)
        {
            if (users.ContainsKey(user.UserName))
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied,
                    "user is already connected"));
            }
            return await Task.FromResult(new Empty());
        }

        public override async Task<Users> UpdateUsers(Empty request,
            ServerCallContext context)
        {
            var reply = new Users
            {
                UserNames = { users.Keys }
            };
            return await Task.FromResult(reply);
        }

        public override async Task<Users> CurrentPlayers(Empty request,
            ServerCallContext context)
        {//TODO go back here
            var temp = new List<string>();
            foreach (var user in users.Keys)
            {
                foreach(var item in users[user])
                {
                    GameMessage msg = item as GameMessage;
                    if (msg.Type == MessageType.Answer && msg.Message == "No")
                    {
                        temp.Remove(msg.FromUser);
                        temp.Remove(msg.ToUser);
                    }
                    else if(msg.Type == MessageType.Invite)
                    {
                        temp.Add(msg.FromUser);
                        temp.Add(msg.ToUser);
                    } 
                }
            }
            foreach (var item in currentGames.Keys)
            {
                temp.Add(item.Blue);
                temp.Add(item.Red);
            }
            var reply = new Users
            {
                UserNames = { temp }
            };
            return await Task.FromResult(reply);
        }
        public override async Task<Empty> Insert(PlayerModel player2add, ServerCallContext context)
        {
            Player newPlayer = new Player
            {
                UserId = GetNewPlayerId(),
                Name = player2add.Name,
                Score = 0,
                Password = EncryptPW(player2add.Password)
            };

            try
            {
                using (var ctx = new fourinrow_gilad_ilyaContext())
                {
                    if (ExistsAlready(newPlayer, ctx))
                        throw new RpcException(new Status(StatusCode.AlreadyExists, "Username already exists"));
                    ctx.Players.Add(newPlayer);
                    ctx.SaveChanges();
                }
            }
            catch (RpcException ex)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
            }
            return await Task.FromResult(new Empty());
        }

        

        private static int gameRunning = 0;
        public override async Task<Empty> GameStarted(GamePlayers players, ServerCallContext context)
        {
            int tempGameId = gameRunning++;
            currentGames.TryAdd(players, tempGameId);
            InformAllUsers(new GameMessage
            {
                Type = MessageType.Update,
                FromUser = "",
                ToUser = "",
                Message = "",
                Score = 0
            });
            //DeclineOtherInvitations(players.Blue);
            //DeclineOtherInvitations(players.Red);
            return await Task.FromResult(new Empty());
        }

        //private static void DeclineOtherInvitations(string player)
        //{
        //    int size = users[player].Count;
        //    for(int i = size; i >= 0; i--)
        //    {
        //        GameMessage note = users[player].ElementAt(i);
        //    //}
        //    //foreach (GameMessage note in users[player])
        //    //{
        //        if (note.Type == MessageType.Invite)
        //        {
        //            users[note.FromUser].Add(new GameMessage
        //            {
        //                Type = MessageType.Answer,
        //                FromUser = note.ToUser,
        //                ToUser = note.FromUser,
        //                Message = "No"
        //            });
        //            users[player].Remove(note);
        //        }
        //    }
        //}


        //        while (!token.IsCancellationRequested)
        //            {
        //                if (users[user.UserName].Count > 0)
        //                {
        //                    foreach (var item in users[user.UserName])
        //                    {
        //                        await responseStream.WriteAsync(item);
        //    }
        //}
        //users[user.UserName].Clear();
        //await Task.Delay(interval, token);
        //            }

        public override async Task<Empty> GameEnded(GamePlayers players, ServerCallContext context)
        {
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                var blue = (from b in ctx.Players
                            where b.Name.Equals(players.Blue)
                            select b).First();
                var red = (from r in ctx.Players
                           where r.Name.Equals(players.Red)
                           select r).First();
                blue.Score += players.BlueScore;
                red.Score += players.RedScore;
                DateTime now = DateTime.Now;
                var game = new Models.Game
                {
                    BlueScore = players.BlueScore,
                    RedScore = players.RedScore,
                    EndTime = now,
                    SquaresMarked = players.Turns,
                    BluePlayerUser = blue,
                    BluePlayerUserId = blue.UserId,
                    RedPlayerUser = red,
                    RedPlayerUserId = red.UserId,
                };
                ctx.Games.Add(game);
                blue.GameBluePlayerUsers.Add(game);
                red.GameRedPlayerUsers.Add(game);
                ctx.SaveChanges();
                AddGame2Players(players, blue, red, now);
            }
            //return await ReturnPlayers2Lobby(players, context);
            return await Task.FromResult(new Empty());
        }
        public override async Task<Empty> ReturnPlayers2Lobby(GamePlayers players, ServerCallContext context)
        {
            int val;
            gameRunning--;
            players.BlueScore = players.RedScore = players.Turns = 0;
            Console.WriteLine("CURRENT GAMES1:: " + currentGames.Count.ToString());
            currentGames.TryRemove(players, out val);
            Console.WriteLine("CURRENT GAMES2:: " + currentGames.Count.ToString());
            InformAllUsers(new GameMessage
            {
                Type = MessageType.Update,
                FromUser = "",
                ToUser = "",
                Message = "",
                Score = 0
            });
            return await Task.FromResult(new Empty());
        }
        
        public override async Task<Games> CurrentGames(Empty empty, ServerCallContext context)
        {
            var reply = new Games
            {
                GamesData = { currentGames.Keys }
            };
            return await Task.FromResult(reply);
        }
        private static void AddGame2Players(GamePlayers players,
            Player blue, Player red, DateTime now)
        {
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                var game = (from g in ctx.Games
                            where g.BluePlayerUser.Equals(blue) &&
                            g.RedPlayerUser.Equals(red) &&
                            g.EndTime.Equals(now)
                            select g).First();
                blue.GameBluePlayerUsers.Add(game);
                red.GameRedPlayerUsers.Add(game);
                ctx.SaveChanges();
                if (players.BlueScore >= 1000)
                {
                    game.WinnerUserId = blue.UserId;
                    blue.GameWinnerUsers.Add(game);
                }
                else if (players.RedScore >= 1000)
                {
                    game.WinnerUserId = red.UserId;
                    red.GameWinnerUsers.Add(game);
                }
                ctx.SaveChanges();
            }
        }

        public override async Task<Players> GetPlayersSorted(SortMessage message, ServerCallContext context)
        {
            Players players = null;
            IOrderedQueryable<PlayerModel> query = null;
            using (var ctx = new fourinrow_gilad_ilyaContext())
            {
                switch (message.Method) {
                    case SortType.Name:
                        query = ((IOrderedQueryable<PlayerModel>)(from p in ctx.Players
                                 orderby p.Name ascending
                                 select new PlayerModel { 
                                    Name = p.Name, 
                                    Total = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count, 
                                    Won = p.GameWinnerUsers.Count,
                                    Lost = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count,
                                    Score = p.Score,
                                    PlayerId = p.UserId
                                 }));
                        break;
                    case SortType.Total:
                        query = ((IOrderedQueryable<PlayerModel>)(from p in ctx.Players
                                 orderby (p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count) descending
                                 select new PlayerModel
                                 {
                                     Name = p.Name,
                                     Total = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count,
                                     Won = p.GameWinnerUsers.Count,
                                     Lost = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count,
                                     Score = p.Score,
                                     PlayerId = p.UserId
                                 }));
                        break;
                    case SortType.Victories:
                        query = ((IOrderedQueryable<PlayerModel>)(from p in ctx.Players
                                 orderby p.GameWinnerUsers.Count descending
                                 select new PlayerModel
                                 {
                                     Name = p.Name,
                                     Total = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count,
                                     Won = p.GameWinnerUsers.Count,
                                     Lost = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count,
                                     Score = p.Score,
                                     PlayerId = p.UserId
                                 }));
                        break;
                    case SortType.Losses:
                        query = ((IOrderedQueryable<PlayerModel>)(from p in ctx.Players
                                 orderby (p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count)
                                 descending
                                 select new PlayerModel
                                 {
                                     Name = p.Name,
                                     Total = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count,
                                     Won = p.GameWinnerUsers.Count,
                                     Lost = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count,
                                     Score = p.Score,
                                     PlayerId = p.UserId
                                 }));
                        break;
                    case SortType.Score:
                        query = ((IOrderedQueryable<PlayerModel>)(from p in ctx.Players
                                 orderby p.Score descending
                                 select new PlayerModel
                                 {
                                     Name = p.Name,
                                     Total = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count,
                                     Won = p.GameWinnerUsers.Count,
                                     Lost = p.GameBluePlayerUsers.Count + p.GameRedPlayerUsers.Count - p.GameWinnerUsers.Count,
                                     Score = p.Score,
                                     PlayerId = p.UserId
                                 }));
                        break;
                }
                players = setPlayers(query);
            }
            return await Task.FromResult(players);
        }

        private Players setPlayers(IOrderedQueryable<PlayerModel> query)
        {
            Players players = new Players();
            foreach (var item in query)
            {
                players.Players_.Add(item);
            }
            return players;
        }

        //encrypting PW using SHA256
        private string EncryptPW(string password)
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                byte[] hashValue = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(password));
                return System.Text.Encoding.Default.GetString(hashValue);
            }
        }

        private bool ExistsAlready(Player newPlayer, fourinrow_gilad_ilyaContext ctx)
        {
            var dupQuery = (from p in ctx.Players
                            where p.Name == newPlayer.Name
                            select p);
            return dupQuery.Count() > 0;

        }

        private int GetNewPlayerId()
        {
            try
            {
                using (var ctx = new fourinrow_gilad_ilyaContext())
                {
                    var pid = (from p in ctx.Players
                               orderby p.UserId descending
                               select p);
                    if (pid == null || pid.Count() == 0)
                        return 0;
                    return pid.First().UserId + 1;
                }
            }
            catch (Exception)
            {
                throw new DbUpdateException("Couldn't set a new ID"); //by adding will be catched and shown to the user
            }
        }


    }
}
