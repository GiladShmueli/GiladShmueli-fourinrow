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
        
        public void StartGame(Color myColor, string myOpponent)
        {
            DrawBoard();
            this.color = myColor;
            brush = new SolidColorBrush(color);
            opponent = myOpponent;
            tbMyColor.Text = "You are " + (MyColor == Colors.Blue? "blue" : "red");
            tbMyColor.Foreground = brush;
            Ellipse myEllipse = new Ellipse
            {
                Width = 60,
                Height = 60,
                Fill = new SolidColorBrush(MyColor == Colors.Blue ? Colors.Blue : Colors.Red)
            };
            Canvas.SetLeft(myEllipse, 0);
            Canvas.SetTop(myEllipse, 0);
            canvasMyColor.Children.Add(myEllipse);
        }

        public (int x, int i) AnimateCircle(int x, Ellipse circle)
        {
            int i;
            for(i=5; i>=0; i--)
            {
                if(board[x,i] == 0)
                {
                    board[x, i] = IsMyTurn() ? ME : RIVAL;
                    y = 10 + 60 * i; //the bottom of the column currently
                    myCanvas.Children.Add(circle);
                    //Thread animThread = new Thread(SlideCircle);
                    //animThread.Start(circle);
                    ThreadPool.QueueUserWorkItem(SlideCircle, circle);
                    return (x, i);
                }
            }
            if (board[x,0] != 0)
            {
                throw new ArgumentOutOfRangeException("This column is full! Try again!");
            }
            return (x, i);
        }

        private bool animate = false;
        public bool Animate { get { return animate; } }
        private int y = 10;
        private void SlideCircle(object obj)
        {
            animate = true;
            Ellipse circle = obj as Ellipse;
            for(int i=0; i <= y; i+= 5) //maybe more than ++
            {
                Thread.Sleep(15);
                Dispatcher.Invoke(() => Canvas.SetTop(circle, i));
            }
            animate = false;
        }

        //printing the board. 1 is me, 2 is opponent, 0 is empty. (printed sideways)
        private string PrintBoard()
        {
            string result = "";
            for(int i=0; i<7; i++)
            {
                for(int j=0; j<6; j++)
                {
                    result += board[i, j].ToString() + " ";
                }
                result += "\n";
            }
            return result;
        }


        /*click =>
        is it my turn? assume yes --
                check where to put +
                if valid +
	                tell my rival where ++
	                turns++ ++
                if not keep my turn
        */
        private async void myCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(animate)
            {
                return;
            }
            if (gameEnded == true)
                return;
            if (!IsMyTurn())
            {
                gameData.Text = "Wait for your turn!";
                return;
            }
            
            Point p = Mouse.GetPosition(myCanvas);
            Ellipse el = new Ellipse
            {
                Height = 40, Width = 40,
                Fill = brush
            };

            int pX = (int)(p.X / (myCanvas.Width / 7));
            Canvas.SetLeft(el, pX * (myCanvas.Width/7) + 10);

            try
            {
                (int x, int y) = AnimateCircle(pX, el); //also validates if the move is valid
                string str;
                
                (gameEnded, str) = CheckIfGameEnded(x, y);
                await SendMoveDelegate(x.ToString(), opponent, this.Score);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("This column is full!", "Invalid Move", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool IsMyTurn()
        {
            if (turns%2 == 0 && this.color==Colors.Blue)
                return true;
            if (turns % 2 == 1 && this.color == Colors.Red)
                return true;
            return false;
        }

        public (bool, string) CheckIfGameEnded(int x, int y)
        {//TODO: POINTER
            turns++;
            if (CheckVictory(x, y).Item1)
            {
                MessageBox.Show(CheckVictory(x, y).Item2, (board[x,y]==ME)?"You won!!! :D" : "You lost... :'(");
                SetLobbyVisibilityDelegate(true, CheckVictory(x,y).Item2 + " the last game");
                return CheckVictory(x, y);
            }
            if (turns == 42)
            {
                score = CalcScore(DRAW);
                MessageBox.Show("It's a draw!");
                SetLobbyVisibilityDelegate(true, $"{Username} vs. {Opponent}: last game was a draw!");
                return CheckVictory(x, y);
            }
            
            if (turns % 2 == 0)
            {
                gameData.Text = this.color==Colors.Blue? $"{Username}'s turn":$"{opponent}'s turn";
                gameData.Foreground = Brushes.Blue;
            }
            else
            {
                gameData.Text = this.color==Colors.Red? $"{Username}'s turn" : $"{opponent}'s turn";
                gameData.Foreground = Brushes.Red;
            }
            return (false, "");
        }
        
        ////make the winning color blink on screen
        //private void BlinkWinner(object obj)
        //{
        //    animate = true;
        //    Color clr = (Color) obj;
        //    Brush winner = new SolidColorBrush(clr);
        //    for (int i = 0; i <= y; i += 5) //maybe more than ++
        //    {
        //        Thread.Sleep(25);
        //        Dispatcher.Invoke(() => 
        //        {
        //            foreach(var item in myCanvas.Children)
        //            {
        //                Ellipse circle = item as Ellipse;
        //                if(circle.Fill == winner)
        //                {
        //                    circle.Fill.Opacity += 0xAA;
        //                }
        //            }
        //        });
        //        Thread.Sleep(25);
        //        Dispatcher.Invoke(() => 
        //        {
        //            foreach(var item in myCanvas.Children)
        //            {
        //                Ellipse circle = item as Ellipse;
        //                if(circle.Fill == winner)
        //                {
        //                    circle.Fill.Opacity -= 0xAA;
        //                }
        //            }
        //        });
        //    }
        //    animate = false;
        //}

        public Func<string, string, int, Task> SendMoveDelegate;

        #region boardRegion
        private void DrawBoard()
        {
            for (int i = 0; i < 8; i++)
            {
                myCanvas.Children.Add(GetVerticalLine(i));
            }
            for (int i = 0; i < 7; i++)
            {
                myCanvas.Children.Add(GetHorizontalLine(i));
            }
        }

        private void CleanBoard()
        {
            myCanvas.Children.Clear();
            board = new int[7, 6];
            gameEnded = false;
            turns = 0;
        }
        private Line GetVerticalLine(int i)
        {
            Line line = new Line();
            line.Stroke = Brushes.Green;
            line.StrokeThickness = 1;
            line.X1 = i * myCanvas.Width / 7;
            line.X2 = line.X1;
            line.Y1 = 0;
            line.Y2 = myCanvas.Height;
            return line;
        }
        private Line GetHorizontalLine(int i)
        {
            Line line = new Line();
            line.Stroke = Brushes.Green;
            line.StrokeThickness = 1;
            line.X1 = 0;
            line.X2 = myCanvas.Width;
            line.Y1 = i * myCanvas.Height / 6;
            line.Y2 = line.Y1;
            return line;
        }
        #endregion
        
        #region victoryCheck
        private (bool, string) CheckVictory(int x, int y)
        {
            int currentPlayer = board[x, y];
            string gameOver = (currentPlayer == 1) ? $"{Username} won" : $"{opponent} won";
            
            if (CheckVictoryHelper(Direction.UpLeft, x - 1, y - 1, currentPlayer) + 
                CheckVictoryHelper(Direction.DownRight, x + 1, y + 1, currentPlayer) >= 3)
            {
                CalcScore(currentPlayer);
                return (true, gameOver);
            }
            if (CheckVictoryHelper(Direction.Left, x - 1, y, currentPlayer) + 
                CheckVictoryHelper(Direction.Right, x + 1, y, currentPlayer) >= 3)
            {
                CalcScore(currentPlayer);
                return (true, gameOver);
            }
            if (CheckVictoryHelper(Direction.DownLeft, x - 1, y + 1, currentPlayer) + 
                CheckVictoryHelper(Direction.UpRight, x + 1, y - 1, currentPlayer) >= 3)
            {
                CalcScore(currentPlayer);
                return (true, gameOver);
            }
            if (CheckVictoryHelper(Direction.Down, x, y + 1, currentPlayer) >= 3)
            {
                CalcScore(currentPlayer);
                return (true, gameOver);
            }
            return (false,"");
        }

        public int CalcScore(int currentPlayer)
        {
            score = 0;
            if (currentPlayer == ME)
            {
                score += 1000;
            }
            else
            {
                for (int i = 6; i >= 0; i--)
                {
                    for (int j = 5; j >= 0; j--)
                    {
                        if(board[i,j] == ME)
                            score+= 10;
                    }
                }
            }
            if (CheckColumnsBonus())
                score += 100;
            return score;
        }

        private bool CheckColumnsBonus()
        {
            bool colFlag = false;
            for (int i = 6; i >= 0; i--)
            {
                colFlag = false;
                for (int j = 5; j >= 0; j--)
                {
                    if (board[i, j] == ME)
                    {
                        colFlag = true;
                        break;
                    }
                }
                if (!colFlag)
                    break;
            }

            return colFlag;
        }

        private int CheckVictoryHelper(Direction direction, int x, int y, int player)
        {
            if (x > 6 || x < 0 || y > 5 || y < 0 )
            {
                return 0;
            }
            if (board[x, y] != player)
                return 0;
            switch (direction)
            {
                case Direction.UpLeft:
                    return 1 + CheckVictoryHelper(direction, x - 1, y - 1, player);
                case Direction.UpRight:
                    return 1 + CheckVictoryHelper(direction, x + 1, y - 1, player);
                case Direction.Left:
                    return 1 + CheckVictoryHelper(direction, x - 1, y, player);
                case Direction.Right:
                    return 1 + CheckVictoryHelper(direction, x + 1, y, player);
                case Direction.DownLeft:
                    return 1 + CheckVictoryHelper(direction, x - 1, y + 1, player);
                case Direction.Down:
                    return 1 + CheckVictoryHelper(direction, x, y + 1, player);
                case Direction.DownRight:
                    return 1 + CheckVictoryHelper(direction, x + 1, y + 1, player);
            }
            return 0;
        }
        #endregion

       
    }
}
