using Grpc.Core;
using grpc4InRowService;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace grpc4InRowClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class GameWindow : Window
    {
        public readonly int ME = 1;
        public readonly int RIVAL = 2;
        public readonly int DRAW = 0;

        private int[,] board = new int[7, 6];
        private Color color;
        private SolidColorBrush brush;
        private string opponent;
        private int turns = 0;
        private bool gameEnded = false;
        private bool closedWindow = false;
        private int score;
        public int Score { get { return score; } }
        public string Username { get; set; }
        public Color MyColor { get { return color; } }
        public string Opponent { get { return opponent; } }
        public int Turns { get { return turns; } }
        public bool ClosedWindow { get { return closedWindow; } set { closedWindow = value; } }
        private enum Direction
        {
            UpLeft, UpRight, Left, Right, DownLeft, Down, DownRight
        };
        public GameWindow(string user, Color myColor, string myOpponent)
        {
            InitializeComponent();
            Username = user;
            this.Show();
            this.Title = $"{Username} VS {myOpponent}";
            StartGame(myColor, myOpponent);
        }

        public Func<bool, string, Task> SetLobbyVisibilityDelegate;
        public Func<string, int, Task> GameInterrupted;
        public Func<GamePlayers, Task> ReturnPlayers2Lobby;

        private async void Window_Closed(object sender, EventArgs e)
        {
            //MessageBox.Show(ContextMenuClosingEvent.HandlerType.ToString() +
            //    "\n   " + sender.GetType() + e.);
            GamePlayers players = new GamePlayers
            {
                Blue = (MyColor == Colors.Blue) ? Username : Opponent,
                Red = (MyColor == Colors.Red) ? Username : Opponent,
                BlueScore = 0,
                RedScore = 0,
                Turns = Turns
            };
            if (!ClosedWindow)
            {
                score = CalcScore(RIVAL);
                await GameInterrupted(opponent, score);
                SetLobbyVisibilityDelegate(true, $"Technical loss:\n" +
                                                 $"{opponent} has won the last game.\n" +
                                                 $"You have forfeited the game.");
            }
            await ReturnPlayers2Lobby(players);
        }
    }




    //functions and fields related to game window in lobby
    public partial class LobbyWindow : Window
    {
        private GameWindow gameWindow = null;

        public string Password { get; internal set; }

        public async Task SendGameMove(string x_loc, string opponent, int score)
        {
            try
            {
                await Client.SendMessageAsync(new GameMessage
                {
                    Type = MessageType.Play,
                    FromUser = Username,
                    ToUser = opponent,
                    Message = x_loc,
                    Score = score,
                });
            }
            catch (RpcException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private Task SetVisibility(bool flag, string gameOverString)
        {
            this.Visibility = Visibility.Visible;
            buttonSend.IsEnabled = true;
            gameData.Text = gameOverString;
            gameWindow.ClosedWindow = flag;
            gameWindow.Close();
            return Task.FromResult(new Empty());
        }

        private async Task GameInterrupted(string toUser, int score)
        {
            _ = await Client.SendMessageAsync(new GameMessage
            {
                Type = MessageType.Endgame,
                FromUser = Username,
                ToUser = toUser,
                Message = "Interrupted",
                Score = score
            });
            this.Visibility = Visibility.Visible;
        }

        private async Task ReturnPlayersFromGame(GamePlayers players)
        {
            await Client.ReturnPlayers2LobbyAsync(players);
        }
    }
}
