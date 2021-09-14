using Grpc.Core;
using grpc4InRowService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace grpc4InRowClient
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        public SearchWindow()
        {
            InitializeComponent();
        }

        public Func<string, Task<List<PlayerModel>>> GetPlayersSorted;
        public Func<Task<List<GamePlayers>>> GetGames;
        public Func<PlayerModel, PlayerModel, Task<List<GamePlayers>>> GetGamesOfTwoPlayers;
        List<PlayerModel> players;
        private async void SortPlayers_Click(object sender, RoutedEventArgs e)
        {
            var method = (sender as MenuItem).Tag;
            await SortPlayersByMethod(method.ToString());
        }

        public async Task SortPlayersByMethod(string method)
        {
            players = await GetPlayersSorted(method);
            lbUsers.ItemsSource = players;
        }

        private async void ShowGames_Click(object sender, RoutedEventArgs e)
        {
            await ShowGames();
        }

        private async void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            PlayerModel player1, player2;
            switch (lbUsers.SelectedItems.Count)
            {
                case 0:
                    MessageBox.Show("You haven't chosen any player.\nChoose a player to get more information about them\n" +
                            "or pick two to see their games history");
                    break;
                case 1:
                    List<string> playerDetails = new List<string>();
                    player1 = lbUsers.SelectedItem as PlayerModel;
                    tbExtra.Text = $"Info about {player1.Name}";
                    playerDetails.Add($"\nScore: {player1.Score}\n\n" +
                                $"Victories: {player1.Won} \t\t Total Games: {player1.Total}\n\n" +
                                $"Victories Precentage: {100 * player1.Won / player1.Total}%\n");
                    lbExtra.ItemsSource = playerDetails; 
                    break;
                case 2:
                    player1 = lbUsers.SelectedItems[0] as PlayerModel;
                    player2 = lbUsers.SelectedItems[1] as PlayerModel;
                    var games = await GetGamesOfTwoPlayers(player1, player2);
                    if (games.Count == 0)
                    {
                        tbExtra.Text = $"No games were found between {player1.Name} and {player2.Name}";
                        lbExtra.ItemsSource = null;
                        return;
                    }
                    tbExtra.Text = GetPlayersPercentage(player1, player2, games);
                    List<string> gamesDetails = new List<string>();
                    string winner;
                    foreach(var game in games)
                    {
                        winner = (game.BlueScore >= 1000) ? game.Blue : (game.RedScore >= 1000) ? game.Red : "DRAW";
                        gamesDetails.Add($"\nBlue: {game.Blue}\t Blue Score: {game.BlueScore}   Red: {game.Red}\t Red Score: {game.RedScore}\n\n" +
                                            $"End Time: {game.StartTime}   Squares Marked: {game.Turns}    Winner: {winner}\n");
                    }
                    lbExtra.ItemsSource = gamesDetails;
                    break;
                default:
                    MessageBox.Show("You have chosen more than 2 players.\nChoose a player to get more information about them\n" +
                            "or pick two to see their games history");
                    break;
            }
        }

        private string GetPlayersPercentage(PlayerModel player1, PlayerModel player2, List<GamePlayers> games)
        {
            int vic1 = 0;
            int vic2 = 0;
            foreach(var game in games)
            {
                if (game.BlueScore >= 1000)
                {
                    if (game.Blue == player1.Name)
                        vic1++;
                    else
                        vic2++;
                }
                if (game.RedScore >= 1000)
                {
                    if (game.Red == player1.Name)
                        vic1++;
                    else
                        vic2++;
                }
            }
            return $"Total games played: {games.Count} \t {player1.Name} won {100*vic1/games.Count}% \t {player2.Name} won {100*vic2/games.Count}%";
        }
        public async Task ShowGames()
        {
            tbExtra.Text = "Games History";
            var games = await GetGames();
            List<string> gamesList = new List<string>();
            string temp;
            string winner;
            foreach(var game in games)
            {
                winner = (game.BlueScore >= 1000) ? game.Blue : (game.RedScore >= 1000) ? game.Red : "DRAW";
                temp = $"\nBlue: {game.Blue}\t Blue Score: {game.BlueScore}   Red: {game.Red}\t Red Score: {game.RedScore}\n\n"+
                       $"End Time: {game.StartTime}   Squares Marked: {game.Turns}    Winner: {winner}\n";
                gamesList.Add(temp);
            }
            lbExtra.ItemsSource = gamesList;
        }
    }

    //Lobby window related to delegates
    public partial class LobbyWindow
    {
        public async Task<List<PlayerModel>> GetPlayerByOrder(string arg)
        {
            List<PlayerModel> playersList = new List<PlayerModel>();
            SortType type = SortType.Name;
            switch (arg)
            {
                case "name":
                    type = SortType.Name;
                    break;
                case "total_games":
                    type = SortType.Total;
                    break;
                case "victories":
                    type = SortType.Victories;
                    break;
                case "losses":
                    type = SortType.Losses;
                    break;
                case "score":
                    type = SortType.Score;
                    break;
            }
            var orderedPlayers = await Client.GetPlayersSortedAsync(new SortMessage { Method = type });
            foreach(var item in orderedPlayers.Players_)
            {
                playersList.Add(item);
            }

            return playersList;
        }

        public async Task<List<GamePlayers>> GetAllGames()
        {
            List<GamePlayers> gamesList = new List<GamePlayers>();
            var games = await Client.PreviousGamesAsync(new Empty());
            foreach(var item in games.GamesData)
            {
                gamesList.Add(item);
            }
            return gamesList;
        }

        public async Task<List<GamePlayers>> Get2PlayersHistory(PlayerModel player1, PlayerModel player2)
        {
            List<GamePlayers> gamesList = new List<GamePlayers>();
            Players players = new Players();
            players.Players_.Add(player1);
            players.Players_.Add(player2);
            var games = await Client.Get2PlayerHistoryAsync(players);
            foreach (var item in games.GamesData)
            {
                gamesList.Add(item);
            }
            return gamesList;
        }
    }
}
