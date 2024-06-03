using System;
using System.Threading;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Net;

namespace BilliardPavliySoroka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Для игры
        private Ellipse billiardBall;
        private Vector direction;
        private double speed;
        private Point startPoint;
        private DispatcherTimer timer;
        private bool isMoving;

        // Для сервера
        private TcpListener tcpListener;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private bool isServerRunning;

        public MainWindow()
        {
            InitializeComponent();

            StartServer(); // Для работы с WPF



            billiardBall = new Ellipse // Создаем шаширика
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.Black
            };


            Canvas.SetLeft(billiardBall, 100); // Начальное положение
            Canvas.SetTop(billiardBall, 100);


            MyCanvas.Children.Add(billiardBall);


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Timer_Tick;


            isMoving = false;  // Движения шара
        }

        private void MyCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isMoving)
            {

                startPoint = e.GetPosition(MyCanvas); // Запоминаем нач точку
            }
        }

        private void MyCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isMoving)
            {

                Point endPoint = e.GetPosition(MyCanvas); // Получаем кон точку и вычисляем направление и скорость
                direction = endPoint - startPoint;
                direction.Normalize();
                speed = (endPoint - startPoint).Length / 10;


                timer.Start();  // Начинаем движение шара
                isMoving = true;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {

            var newX = Canvas.GetLeft(billiardBall) + direction.X * speed; // Обновляем позицию шара
            var newY = Canvas.GetTop(billiardBall) + direction.Y * speed;


            if (newX <= 0 || newX >= MyCanvas.ActualWidth - billiardBall.Width) // Столкновение со стенками
                direction.X = -direction.X;
            if (newY <= 0 || newY >= MyCanvas.ActualHeight - billiardBall.Height)
                direction.Y = -direction.Y;


            Canvas.SetLeft(billiardBall, newX); // Новая позиция
            Canvas.SetTop(billiardBall, newY);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);


            if (e.Key == Key.Space)    // Конец хода
            {
                if (isMoving)
                {
                    timer.Stop();
                    isMoving = false;
                }
            }
        }

        private void StartServer() //Все для сервера
        {
            tcpListener = new TcpListener(IPAddress.Any, 13);
            tcpListener.Start();
            isServerRunning = true;
            ListenForClients();
        }

        private async void ListenForClients()
        {
            while (isServerRunning)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                connectedClients.Add(client);
                var clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object obj)
        {
            var client = (TcpClient)obj;
            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            while (true)
            {
                try
                {
                    var message = reader.ReadLine();
                    if (message != null)
                    {

                        BroadcastMessage(message, client);
                    }
                }
                catch
                {

                    connectedClients.Remove(client);
                    client.Close();
                    break;
                }
            }
        }

        private void BroadcastMessage(string message, TcpClient originClient)
        {
            foreach (var client in connectedClients)
            {
                if (client != originClient)
                {
                    var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                    writer.WriteLine(message);
                }
            }
        }
    }
}