using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.IO;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                Console.WriteLine("Введите IP или сетевое имя принимаемого компьютера");
                Console.Write("\tIP or Host: ");
                Console.CursorVisible = true;
                string host = Console.ReadLine();
                Console.CursorVisible = false;
                Console.WriteLine();

                SendMessageFromSocket(host);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }


        static void SendMessageFromSocket(string host)
        {

            IPAddress ipAddress = Dns.GetHostEntry(host).AddressList[0];

            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(host), port: 9050);
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Буфер для входящих данных
            byte[] bytes = new byte[1024 * 1024];

            // Соединяем сокет с удаленной точкой
            sender.Connect(ipEndPoint);

            Console.Write("Введите сообщение: ");
            string message = Console.ReadLine();

            if (String.IsNullOrEmpty(message)) { message = "empty"; }
            else
                Console.WriteLine("Сокет соединяется с {0} ", sender.RemoteEndPoint.ToString());
            byte[] msg = Encoding.UTF8.GetBytes(message);

            // Отправляем данные через сокет
            int bytesSent = sender.Send(msg);

            // Получаем ответ от сервера
            int bytesRec = sender.Receive(bytes);

            Console.WriteLine("\nОтвет от сервера: {0}\n\n", Encoding.UTF8.GetString(bytes, 0, bytesRec));


            // прием файла
            if (message == "screen")
            {
                SocketReceive(sender, @"\screen.jpg");

            }

            if (message == "log")
            {
                SocketReceive(sender, @"\log.dat");

            }


            // Используем рекурсию для неоднократного вызова SendMessageFromSocket()
            if (message.IndexOf("quit") == -1)

                SendMessageFromSocket(host);
            Console.WriteLine("Передача завершена. Завершите соединение вручную\n");
        }

        public static void SocketReceive(Socket sender, string fileN)
        {
            string file = Application.StartupPath + fileN;

            byte[] clientData = new byte[1024 * 5000];

            int receivedBytesLen = sender.Receive(clientData);

            int fileNameLen = BitConverter.ToInt32(clientData, 0);
            string fileName = Encoding.ASCII.GetString(clientData, 4, fileNameLen);

            Console.WriteLine("Client:{0} connected & File {1} started received.", sender.RemoteEndPoint, fileName);

            BinaryWriter bWrite = new BinaryWriter(File.Open(file, FileMode.Append)); ;
            bWrite.Write(clientData, 4 + fileNameLen, receivedBytesLen - 4 - fileNameLen);

            Console.WriteLine("File received & saved at path: {1}", fileName, file);

            bWrite.Close();
        }
    }
}
