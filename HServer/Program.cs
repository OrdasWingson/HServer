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
        static string directory;
        static MemoryStream ms = new MemoryStream(new byte[256 * 100], 0, 256 * 100, true, true);//буфер для данных     
        static BinaryReader reader = new BinaryReader(ms);//чтение из потока
        static BinaryWriter writer = new BinaryWriter(ms);//запись в поток
        static Thread server = new Thread(threadServer);
        static void Main(string[] args)
        {
            Console.Title = "HServer";
            socket.Bind(new IPEndPoint(IPAddress.Any, 8080));//привязываем сокет к порту
            socket.Listen(0);//прослушиваем сокет
            
            server.Start();
            socket.BeginAccept(AcceptCallBack, null);//ассинхронное подключение
            while(true)
            {

            }
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
            //MemoryStream ms = new MemoryStream(new byte[256*100], 0, 256*100, true, true);//буфер для данных            
            BinaryReader reader = new BinaryReader(ms);//чтение из потока
            BinaryWriter writer = new BinaryWriter(ms);//запись в поток

            #region intoduce
            ms.Position = 0;            
            client.Socket.Receive(ms.GetBuffer());
            client.userName = reader.ReadString();
            client.machineName = reader.ReadString();
            client.currentDirectory = directory = reader.ReadString();
            #endregion


            //while (true)
            //{
            //    Console.Write(client.currentDirectory + ":> ");
            //    string request = Console.ReadLine();//ждем запрос от юзера
            //    if (request.Equals("cc"))
            //    {
            //        if (clients.Count != 0)
            //        {
            //            Console.WriteLine("Клиенты:");
            //            var count = 0;
            //            foreach (var cl in clients)
            //            {
            //                Console.WriteLine(count + " => " + cl.userName + " IP => " + cl.Socket.RemoteEndPoint.ToString());
            //                count++;
            //            }
            //            Console.Write("Введите номер клиента:> ");
            //            int numb = Convert.ToInt32(Console.ReadLine());
            //            client = clients[numb];
            //            Console.WriteLine("Вв > " + numb);
            //            continue;
            //        }

            //    }
            //    ms.Position = 0;//выставляем позицию в буфере
            //    writer.Write(request);//записываем запрос
            //    client.Socket.Send(ms.GetBuffer());      //отсылаем запрос клиенту         
            //    client.Socket.Receive(ms.GetBuffer()); //получаем ответ от клиента
            //    ms.Position = 0;//выставляем позицию в буфере
            //    int code = reader.ReadInt32();//считываем код ответа

            //    switch (code)//выполняем действие согласно коду
            //    {
            //        case 1: //получем файлы из дерриктории dir
            //            getDirectory(client, ms);
            //            break;
            //        case 2: //переходим в другую директорию cd
            //            changeDirectory(client, ms);
            //            break;
            //        case 13: //переходим в другую директорию cd
            //            erroreCode(ms);
            //            break;
            //        default:
            //            Console.WriteLine("Default case");
            //            break;
            //    }


            //}
        }

        private static void threadServer()
        {
            Client client = new Client();     
            while (true)
            {
                if(clients.Count==0)
                {                    
                    continue;
                }
               
                Console.Write(client.currentDirectory + ":> ");
                string request = Console.ReadLine();//ждем запрос от юзера
                if (request.Equals("cc"))
                {
                    if (clients.Count != 0)
                    {
                        Console.WriteLine("Клиенты:");
                        var count = 0;
                        foreach (var cl in clients)
                        {
                            Console.WriteLine(count + " => " + cl.userName + " IP => " + cl.Socket.RemoteEndPoint.ToString());
                            count++;
                        }
                        Console.Write("Введите номер клиента:> ");
                        int numb = Convert.ToInt32(Console.ReadLine());
                        client = clients[numb];                        
                        continue;
                    }

                }
                ms.Position = 0;//выставляем позицию в буфере
                writer.Write(request);//записываем запрос
                client.Socket.Send(ms.GetBuffer());      //отсылаем запрос клиенту         
                client.Socket.Receive(ms.GetBuffer()); //получаем ответ от клиента
                ms.Position = 0;//выставляем позицию в буфере
                int code = reader.ReadInt32();//считываем код ответа

                switch (code)//выполняем действие согласно коду
                {
                    case 1: //получем файлы из дерриктории dir
                        getDirectory(client, ms);
                        break;
                    case 2: //переходим в другую директорию cd
                        changeDirectory(client, ms);
                        break;
                    case 13: //переходим в другую директорию cd
                        erroreCode(ms);
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }


            }
        }

        private static void erroreCode(MemoryStream ms)
        {
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            Console.WriteLine(reader.ReadString());
        }

        private static void changeDirectory(Client client, MemoryStream ms)//смена директории
        {
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            client.currentDirectory = reader.ReadString();//записываем новую директорию
            Console.Clear();
            Console.WriteLine(client.currentDirectory + "> ");
        }

        private static void getDirectory(Client client,MemoryStream ms)
        {
            
            BinaryReader reader = new BinaryReader(ms);//чтение из потока           
            int count = reader.ReadInt32();
            Console.Clear();           

            Console.WriteLine(client.currentDirectory + "> ");
            for (int i = 0; i < count; i++)
            {                
                Console.WriteLine("      "+ reader.ReadString());                
            }           
        }
       
    }

    class Client
    {
        public Socket Socket { get; set; }
        public string userName { get; set; }
        public string machineName { get; set; }
        public string currentDirectory { get; set; }
        public Client(Socket s)
        {
            Socket = s;
        }

        public Client()
        {           
            userName = "null";
            machineName = "null";
            currentDirectory = "null";
                    
        }
    }
}
