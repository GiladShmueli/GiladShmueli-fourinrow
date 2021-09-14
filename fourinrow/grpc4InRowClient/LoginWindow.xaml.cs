using Grpc.Core;
using Grpc.Net.Client;
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //MessageBox.Show("Welcome!");
            InitializeComponent();
        }

        public static Game.GameClient Client { get; private set; }
        public static string Username { get; private set; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbUsername.Text))
            {
                var channel = GrpcChannel.ForAddress("https://localhost:5001"); //TODO no service
                Game.GameClient client = new Game.GameClient(channel);
                string username = tbUsername.Text.Trim();
                string password = tbPassword.Password.Trim();
                try
                {
                    await client.UserInDBAsync(new UserInfo { UserName = username, Password = password }); //error status - not found
                    await client.UserConnectedAsync(new UserInfo { UserName = username, Password = password }); //error status - permission denied
                }
                catch (RpcException ex)
                {
                    switch (ex.StatusCode)
                    {
                        case StatusCode.NotFound:
                            MessageBox.Show("User is not found", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                            return;
                        case StatusCode.PermissionDenied:
                            MessageBox.Show("User is already connected", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                            return;
                        case StatusCode.Unauthenticated:
                            MessageBox.Show("Password does not fit the username", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                            return;
                        default:
                            MessageBox.Show("The service is currently unavailable.", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                            return;
                    }
                }
                AsyncServerStreamingCall<GameMessage> listener =
                    client.Connect(new UserInfo { UserName = username, Password = password });
                LobbyWindow mainWindow = new LobbyWindow();
                mainWindow.Client = client;
                mainWindow.Username = username;
                mainWindow.Password = password;
                mainWindow.Title = username;
                mainWindow.ListenAsync(
                    listener.ResponseStream, new CancellationTokenSource().Token);
                this.Close();
                mainWindow.Show();
            } else
            {
                if (string.IsNullOrEmpty(tbUsername.Text.Trim()) || string.IsNullOrEmpty(tbPassword.Password.Trim()))
                {
                    MessageBox.Show("Error: username or password was left\n" +
                        "empty or contains only whitespaces.\n" +
                        "Please make sure both contain at least one non-whitespace character each!", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string name = tbUsername.Text.Trim();
            string pw = tbPassword.Password.Trim();
            if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pw))
            {
                MessageBox.Show("Error: username or password was left\n"+
                    "empty or contains only whitespaces.\n"+
                    "Please make sure both contain at least one non-whitespace character each!", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
                return;
            }
            PlayerModel p2a = new PlayerModel
            {
                Name = name,
                Password = pw
            };
            await AddPlayer2DB(p2a);
        }

        private async Task AddPlayer2DB(PlayerModel player2add)
        {
            try
            {
                var channel = GrpcChannel.ForAddress("https://localhost:5001");
                Game.GameClient client = new Game.GameClient(channel);
                await client.InsertAsync(player2add);
                MessageBox.Show("You registered successfully");
            }
            catch (RpcException ex)
            {
                MessageBox.Show("Username already exists in the database", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
        }
    }
}
