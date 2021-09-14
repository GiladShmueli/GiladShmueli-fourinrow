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
    public partial class LobbyWindow : Window
    {
        public LobbyWindow()
        {
            InitializeComponent();
        }

        public Game.GameClient Client { get; set; }
        public string Username { get; set; }
        public async Task ListenAsync(IAsyncStreamReader<GameMessage> stream, CancellationToken token)
        {
            await foreach (GameMessage info in stream.ReadAllAsync(token))
            {
                if (info.Type == MessageType.Update)
                {
                    await UpdateUsersListAsync();
                }
                else if (info.Type == MessageType.Answer)
                {
                    if(info.Message == "Yes")
                    {
                        gameWindow = new GameWindow(Username, Colors.Blue, info.FromUser);
                        gameWindow.SendMoveDelegate += SendGameMove;
                        gameWindow.SetLobbyVisibilityDelegate += this.SetVisibility;
                        gameWindow.GameInterrupted += GameInterrupted;
                        gameWindow.ReturnPlayers2Lobby += ReturnPlayersFromGame;
                        this.Visibility = Visibility.Hidden;
                        await Client.GameStartedAsync(new GamePlayers
                        {
                            Blue = info.ToUser, 
                            Red = info.FromUser
                        });
                    }
                    else
                    {
                        MessageBox.Show($"{info.FromUser} has declined your invitation.");
                        buttonSend.IsEnabled = true;
                    }
                }
                else if (info.Type==MessageType.Invite)
                {
                    string temp = this.Title;
                    this.Title += " - you have been invited";
                    buttonSend.IsEnabled = false;
                    var result = MessageBox.Show($"{info.FromUser} has invited you to a game.\nWould you like to play, {Username}?","Game Invitation",
                        MessageBoxButton.YesNo);
                    if(result.ToString() == "Yes")
                    {
                        gameWindow = new GameWindow(Username, Colors.Red, info.FromUser);
                        gameWindow.SendMoveDelegate += SendGameMove;
                        gameWindow.SetLobbyVisibilityDelegate += this.SetVisibility;
                        gameWindow.GameInterrupted += GameInterrupted;
                        gameWindow.ReturnPlayers2Lobby += ReturnPlayersFromGame;
                        this.Visibility = Visibility.Hidden;
                        try
                        {
                            await Client.SendMessageAsync(new GameMessage
                            {
                                Type = MessageType.Answer,
                                FromUser = info.ToUser,
                                ToUser = info.FromUser,
                                Message = "Yes"
                            });
                        }
                        catch (RpcException ex)
                        {
                            if(ex.StatusCode == StatusCode.Unavailable)
                            {
                                MessageBox.Show("The service is currently unavailable.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            MessageBox.Show(ex.Message);//TODO: what message gets here
                        }
                        this.Title = temp;
                    } 
                    else if (result.ToString() == "No")
                    {
                        await Client.SendMessageAsync(new GameMessage
                        {
                            Type = MessageType.Answer,
                            FromUser = info.ToUser,
                            ToUser = info.FromUser,
                            Message = "No"
                        });
                        this.Title = temp;
                        buttonSend.IsEnabled = true;
                    }
                } 
                else if (info.Type == MessageType.Play)
                {
                    Ellipse el = new Ellipse
                    {
                        Height = 40,
                        Width = 40,
                        Fill = new SolidColorBrush(gameWindow.MyColor == Colors.Blue ? Colors.Red : Colors.Blue)
                    };
                    int x = int.Parse(info.Message);
                    int y;
                    Canvas.SetLeft(el, x * (gameWindow.myCanvas.Width / 7) + 10);
                    (x, y) = gameWindow.AnimateCircle(x, el);
                   
                    (bool gameEnded, string gameOverString) = gameWindow.CheckIfGameEnded(x, y);
                    if (gameEnded)
                    {
                        await EndGame(info, gameOverString + " the last game");
                    }
                }
                else if (info.Type == MessageType.Endgame) //second player closed the game
                {
                    if (info.Message == "Interrupted")
                    {
                        gameWindow.CalcScore(gameWindow.ME);
                        await EndGame(info, $"Technical win:\n" +
                                      $"You have won the last game.\n" +
                                      $"{gameWindow.Opponent} has forfeited the game.");
                    }
                    buttonSend.IsEnabled = true;
                }
            }
        }

        private async Task EndGame(GameMessage info, string gameOverString)
        {
            GamePlayers players = new GamePlayers
            {
                Blue = (gameWindow.MyColor == Colors.Blue) ? Username : gameWindow.Opponent,
                Red = (gameWindow.MyColor == Colors.Red) ? Username : gameWindow.Opponent,
                BlueScore = (gameWindow.MyColor == Colors.Blue) ? gameWindow.Score : info.Score,
                RedScore = (gameWindow.MyColor == Colors.Red) ? gameWindow.Score : info.Score,
                Turns = gameWindow.Turns
            };
            await Client.GameEndedAsync(players);
            //await Client.ReturnPlayers2LobbyAsync(players);
            await gameWindow.SetLobbyVisibilityDelegate(true, gameOverString);
        }

        //private async Task Reconnect()
        //{
        //    await Client.DisconnectAsync(new UserInfo { UserName = this.Username, Password = this.Password });
        //    AsyncServerStreamingCall<GameMessage> listener =
        //                            Client.Connect(new UserInfo { UserName = this.Username, Password = this.Password });
        //    this.ListenAsync(
        //                listener.ResponseStream, new CancellationTokenSource().Token);
        //}

        private async Task UpdateUsersListAsync()
        { 
            var games = (await Client.CurrentGamesAsync(new Empty())).GamesData;
            spGamesNow.Children.RemoveRange(0, spGamesNow.Children.Count);
            foreach (var item in games)
            {
                StackPanel gamePanel = new StackPanel { Orientation = Orientation.Horizontal };
                gamePanel.Children.Add(new TextBlock { Text = $"{item.Blue}", Foreground = Brushes.Blue });
                gamePanel.Children.Add(new TextBlock { Text = " vs. " });
                gamePanel.Children.Add(new TextBlock { Text = $"{item.Red}", Foreground = Brushes.Red });
                gamePanel.Children.Add(new TextBlock { Text = $"{item.StartTime}" });
                spGamesNow.Children.Add(gamePanel);
            }
            var users = (await Client.UpdateUsersAsync(new Empty())).UserNames;
            users.Remove(Username);
            var players = (await Client.CurrentPlayersAsync(new Empty())).UserNames;
            foreach (var item in players)
            {
                users.Remove(item);
            }
            List<string> usersList = new List<string>();
            foreach(var item in users)
            {
                usersList.Add(item);
            }
            usersList.Sort();
            lbUsers.ItemsSource = usersList;
            
        }

        private async void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            if (lbUsers.SelectedItem == null)
            {
                MessageBox.Show("You must select a user to play with");
                return;
            }
            string receiver = lbUsers.SelectedItem as string;
            try
            {
                buttonSend.IsEnabled = false;
                await Client.SendMessageAsync(new GameMessage
                {
                    Type = MessageType.Invite,
                    FromUser = Username,
                    ToUser = receiver,
                    Message = "invite"
                });
                //await UpdateUsersListAsync();
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.Unavailable)
                {
                    MessageBox.Show("The service is currently unavailable.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                MessageBox.Show(ex.Message);
            }
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            await Client.DisconnectAsync(new UserInfo { UserName = Username, Password = this.Password });
            Environment.Exit(Environment.ExitCode);
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            //add delegate/func/action
            SearchWindow searchWindow = new SearchWindow();
            searchWindow.GetPlayersSorted += GetPlayerByOrder;
            searchWindow.GetGames += GetAllGames;
            searchWindow.GetGamesOfTwoPlayers += Get2PlayersHistory;
            searchWindow.Show();
            await searchWindow.SortPlayersByMethod("name");
            await searchWindow.ShowGames();
        }

    }
}
