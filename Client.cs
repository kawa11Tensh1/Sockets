using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // Создаем клиентский сокет
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Получаем IP-адрес и порт для подключения
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8888;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            // Подключаемся к серверу
            clientSocket.Connect(ipEndPoint);

            Console.WriteLine("Вы подключены к серверу!");

            // Получаем приветственное сообщение от сервера
            string message = ReceiveData(clientSocket);
            Console.WriteLine(message);

            // Получаем игровое поле от сервера
            message = ReceiveData(clientSocket);
            Console.WriteLine(message);

            // Начинаем игру
            // Начинаем игру
            bool isGameOver = false;
            while (!isGameOver)
            {
                // Получаем ход от пользователя
                Console.Write("Введите координаты через пробел: ");
                string input = Console.ReadLine();

                // Если пользователь ввел "disconnect", то отключаемся от сервера и выходим из цикла
                if (input == "disconnect")
                {
                    Disconnect(clientSocket);
                    break;
                }

                string[] inputArray = input.Split(' ');
                string coords = inputArray[0] + inputArray[1];

                // Отправляем ход на сервер
                SendData(coords, clientSocket);

                // Получаем обновленное игровое поле от сервера
                message = ReceiveData(clientSocket);
                Console.WriteLine(message);

                // Получаем инструкцию о текущем игроке от сервера
                message = ReceiveData(clientSocket);
                Console.WriteLine(message);

                // Проверяем, закончилась ли игра
                if (message.Contains("победил") || message.Contains("Ничья"))
                {
                    isGameOver = true;
                }
            }

            // Закрываем соединение
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();

            Console.WriteLine("Игра окончена. Нажмите любую клавишу, чтобы выйти...");
            Console.ReadKey();
        }

        // Функция для получения данных от сервера
        static string ReceiveData(Socket socket)
        {
            byte[] buffer = new byte[1024];
            int bytesReceived = socket.Receive(buffer);
            string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            return data;
        }

        // Функция для отправки данных на сервер
        static void SendData(string data, Socket socket)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            socket.Send(buffer);
        }

        // Функция для отключения клиента от сервера
        static void Disconnect(Socket socket)
        {
            // Отправляем на сервер команду на отключение
            string command = "disconnect";
            SendData(command, socket);

            // Закрываем соединение
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            // Выводим сообщение об успешном разрыве соединения
            Console.WriteLine("Соединение разорвано.");
        }
    }
}