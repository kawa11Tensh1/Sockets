using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

namespace CloseConnection
{
    class Program
    {
        // Перечисление состояний соединения
        public enum State
        {
            All = 0,
            Closed = 1,
            Listen = 2,
            Syn_Sent = 3,
            Syn_Rcvd = 4,
            Established = 5,
            Fin_Wait1 = 6,
            Fin_Wait2 = 7,
            Close_Wait = 8,
            Closing = 9,
            Last_Ack = 10,
            Time_Wait = 11,
            Delete_TCB = 12
        }

        // Структура MIB_TCPROW для хранения информации о соединении
        private struct MIB_TCPROW
        {
            public int dwState;
            public int dwLocalAddr;
            public int dwLocalPort;
            public int dwRemoteAddr;
            public int dwRemotePort;
        }

        // API для изменения состояния соединения
        [DllImport("iphlpapi.dll")]
        private static extern int SetTcpEntry(IntPtr pTcprow);

        // Преобразование 16-битного значения из сетевого порядка байтов в хостовый порядок байтов
        [DllImport("wsock32.dll")]
        private static extern int ntohs(int netshort);

        // Преобразование 16-битного значения обратно в сетевой порядок байтов
        [DllImport("wsock32.dll")]
        private static extern int htons(int netshort);

        // Метод для закрытия соединения на основе указанных параметров
        public static void CloseConnection(string localAddress, int localPort, string remoteAddress, int remotePort)
        {
            try
            {
                // Создание структуры MIB_TCPROW для хранения данных о соединении
                MIB_TCPROW row = new MIB_TCPROW();
                row.dwState = 12; // Установка желаемого состояния закрытия соединения
                byte[] bLocAddr = IPAddress.Parse(localAddress).GetAddressBytes();
                byte[] bRemAddr = IPAddress.Parse(remoteAddress).GetAddressBytes();
                row.dwLocalAddr = BitConverter.ToInt32(bLocAddr, 0);
                row.dwRemoteAddr = BitConverter.ToInt32(bRemAddr, 0);
                row.dwLocalPort = htons(localPort);
                row.dwRemotePort = htons(remotePort);

                // Создание указателя на структуру и вызов API SetTcpEntry для изменения состояния соединения
                IntPtr ptr = GetPtrToNewObject(row);
                int ret = SetTcpEntry(ptr);

                // Обработка возможных ошибок API SetTcpEntry
                if (ret == -1) throw new Exception("Unsuccessful");
                if (ret == 65) throw new Exception("User has no sufficient privilege to execute this API successfully");
                if (ret == 87) throw new Exception("Specified port is not in state to be closed down");
                if (ret == 317) throw new Exception("The function is unable to set the TCP entry since the application is running non-elevated");
                if (ret != 0) throw new Exception("Unknown error (" + ret + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("CloseConnection failed (" + localAddress + ":" + localPort + "->" + remoteAddress + ":" + remotePort + ")! [" + ex.GetType().ToString() + "," + ex.Message + "]");
            }
        }

        // Получение указателя на новый объект в памяти
        private static IntPtr GetPtrToNewObject(object obj)
        {
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }

        private static void Main(string[] args)
        {
            IPGlobalProperties IPGproperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = IPGproperties.GetActiveTcpConnections();

            // Получение активных TCP-соединений и вывод информации о них
            foreach (TcpConnectionInformation tcp in connections)
            {
                if (tcp.LocalEndPoint.Port.ToString() == "5566")
                {
                    Console.WriteLine("Local endpoint:\t" + tcp.LocalEndPoint.Address.ToString() + ": " + tcp.LocalEndPoint.Port.ToString());
                    Console.WriteLine("Remote endpoint: " + tcp.RemoteEndPoint.Address.ToString() + ": " + tcp.RemoteEndPoint.Port.ToString());
                    Console.WriteLine("State:\t" + tcp.State.ToString());
                    break;
                }
            }

            // Ввод данных для закрытия соединения
            Console.WriteLine("\nDo you want to close the connection? \nInput \n1) Local IP \n2) Local Port \n3) Remote IP \n4) Remote Port\n");
            string local_IP = Console.ReadLine();
            string local_Port = Console.ReadLine();
            string remote_IP = Console.ReadLine();
            string remote_Port = Console.ReadLine();

            // Закрытие соединения на основе введенных данных
            CloseConnection(local_IP, Int32.Parse(local_Port), remote_IP, Int32.Parse(remote_Port));
        }
    }
}