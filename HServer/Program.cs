using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HServer
{
    


    class Program
    {
        static Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);//создание сокета
        static List<Client> clients = new List<Client>();//список клиентов

        

        static void Main(string[] args)
        {
            Console.Title = "HServer";
            socket.Bind(new IPEndPoint(IPAddress.Any, 8080));//привязываем сокет к порту
            socket.Listen(0);//прослушиваем сокет

            socket.BeginAccept(AcceptCallBack, null);//ассинхронное подключение
            Console.ReadLine();
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            Client client = new Client(socket.EndAccept(ar));//получение сокета клиента            
            Thread thread = new Thread(HandleClient);//создаем поток для каждого клиента
            thread.Start(client);
            clients.Add(client);
            Console.WriteLine("Новое подключение.");
            socket.BeginAccept(AcceptCallBack, null);
        }

        private static void HandleClient(object o)
        {
            Client client = (Client)o;//наш клиен
            MemoryStream ms = new MemoryStream(new byte[256*100], 0, 256*100, true, true);//буфер для данных
            
            BinaryReader reader = new BinaryReader(ms);//чтение из потока
            BinaryWriter writer = new BinaryWriter(ms);//запись в поток
            while (true)
            {
                string request = Console.ReadLine();
                ms.Position = 0;
                writer.Write(request);
                client.Socket.Send(ms.GetBuffer());
                
                client.Socket.Receive(ms.GetBuffer());               
                getDirectory(ms);


                /*ms.Position = 0;//положение курсора в начало
                client.Socket.Receive(ms.GetBuffer());//получаем данные от клиента
                /*int code = reader.ReadInt32();//считываем из буфера данные

               switch(code)
                {
                    case 1:
                        getDirectory(ms);
                        break;
                    case 2:
                        Console.WriteLine("Case 2");
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }*/
                

            }
        }

        private static void getDirectory(MemoryStream ms)
        {
            
            BinaryReader reader = new BinaryReader(ms);//чтение из потока
            ms.Position = 0;
            int count = reader.ReadInt32();
            Console.Clear();
            Console.WriteLine("Папок в директории: " + count);
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine("=> "+ reader.ReadString());
            }
            
        }
    }

    class Client
    {
        public Socket Socket { get; set; }
        public int ID { get; set; }

        public Client(Socket s)
        {
            Socket = s;
        }
    }
}
