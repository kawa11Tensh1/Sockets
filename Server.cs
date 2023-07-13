using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Создаем серверный сокет
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Получаем IP-адрес и порт для прослушивания
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8888;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            // Связываем серверный сокет с IP-адресом и портом
            serverSocket.Bind(ipEndPoint);

            // Начинаем прослушивание входящих подключений
            serverSocket.Listen(10);

            Console.WriteLine("Сервер запущен. Ожидание клиента...");

            // Принимаем входящее подключение
            Socket clientSocket = serverSocket.Accept();

            Console.WriteLine("Клиент подключен!");

            // Отправляем сообщение клиенту
            byte[] message = Encoding.UTF8.GetBytes("Добро пожаловать в игру Крестики-Зерики!\n");
            clientSocket.Send(message);

            // Создаем игровое поле
            char[,] board = new char[3, 3];

            // Заполняем игровое поле начальными значениями
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    board[i, j] = '-';
                }
            }

            // Отправляем игровое поле клиенту
            SendBoard(board, clientSocket);

            // Начинаем игру
            bool isGameOver = false;
            char currentPlayer = 'X';
            while (!isGameOver)
            {
                // Отправляем текущего игрока клиенту
                SendCurrentPlayer(currentPlayer, clientSocket);

                // Проверяем, есть ли победитель
                if (CheckForWinner(board))
                {
                    isGameOver = true;
                    SendGameOverMessage(currentPlayer, "Вы победили!", clientSocket);
                    break;
                }

                // Проверяем, что нет ничьей
                if (CheckForDraw(board))
                {
                    isGameOver = true;
                    SendGameOverMessage('-', "Ничья!", clientSocket);
                    break;
                }

                // Получаем координаты от клиента
                string coords = ReceiveData(clientSocket);

                // Проверяем наличие команды разрыва соединения
                if (coords == "disconnect")
                {
                    // Разрываем соединение с клиентом
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    Console.WriteLine("Соединение разорвано...");
                    break;
                }

                int row = Convert.ToInt32(coords[0].ToString());
                int col = Convert.ToInt32(coords[1].ToString());

                // Проверяем, что клетка свободна
                if (board[row, col] != '-')
                {
                    SendError("Клетка уже занята!", clientSocket);
                    continue;
                }

                // Заполняем клетку
                board[row, col] = currentPlayer;

                // Меняем текущего игрока
                currentPlayer = (currentPlayer == 'X') ? 'O' : 'X';

                // Отправляем игровое поле клиенту
                SendBoard(board, clientSocket);

                // Проверяем наличие команды разрыва соединения после каждого хода
                if (ReceiveData(clientSocket) == "disconnect")
                {
                    // Разрываем соединение с клиентом
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    Console.WriteLine("Соединение разорвано...");
                    break;
                }
            }

            // Закрываем соединение
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

        // Функция для получения данных от клиента
        static string ReceiveData(Socket socket)
        {
            byte[] buffer = new byte[1024];
            int bytesReceived = socket.Receive(buffer);
            string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            return data;
        }

        // Функция для отправки игрового поля клиенту
        static void SendBoard(char[,] board, Socket socket)
        {
            string boardString = "  0 1 2\n";
            for (int i = 0; i < 3; i++)
            {
                boardString += i + " ";
                for (int j = 0; j < 3; j++)
                {
                    boardString += board[i, j] + " ";
                }
                boardString += "\n";
            }
            byte[] message = Encoding.UTF8.GetBytes(boardString);
            socket.Send(message);
        }

        // Функция для отправки текущего игрока клиенту
        static void SendCurrentPlayer(char currentPlayer, Socket socket)
        {
            string message = "Ход игрока " + currentPlayer + "\n";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            socket.Send(buffer);
        }

        // Функция для отправки сообщения об ошибке клиенту
        static void SendError(string errorMessage, Socket socket)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(errorMessage);
                socket.Send(buffer);
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, отправляем сообщение с кодом ошибки
                string errorResponse = $"Ошибка {ex.HResult}: {ex.Message}";
                byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                socket.Send(buffer);
            }
        }

        // Функция для отправки сообщения о завершении игры клиенту
        static void SendGameOverMessage(char winner, string message, Socket socket)
        {
            string winnerMessage = (winner == '-') ? "" : "Победитель: " + winner + "\n";
            byte[] buffer = Encoding.UTF8.GetBytes(winnerMessage + message);
            socket.Send(buffer);
        }

        // Функция для проверки наличия победителя
        static bool CheckForWinner(char[,] board)
        {
            // Проверка диагоналей
            if ((board[0, 0] != '-') && (board[0, 0] == board[1, 1]) && (board[1, 1] == board[2, 2]))
            {
                return true;
            }
            if ((board[2, 0] != '-') && (board[2, 0] == board[1, 1]) && (board[1, 1] == board[0, 2]))
            {
                return true;
            }

            // Проверка строк и столбцов
            for (int i = 0; i < 3; i++)
            {
                if ((board[i, 0] != '-') && (board[i, 0] == board[i, 1]) && (board[i, 1] == board[i, 2]))
                {
                    return true;
                }
                if ((board[0, i] != '-') && (board[0, i] == board[1, i]) && (board[1, i] == board[2, i]))
                {
                    return true;
                }
            }

            return false;
        }

        // Функция для проверки наличия ничьей
        static bool CheckForDraw(char[,] board)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == '-')
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}