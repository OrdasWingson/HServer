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

        static Dictionary<string, int> code = new Dictionary<string, int>()//команда и код
        {
            {"cc", 1},
            {"upload", 2},            
        };


        static void Main(string[] args)
        {
            Console.Title = "HServer";
            socket.Bind(new IPEndPoint(IPAddress.Any, 8080));//привязываем сокет к порту
            server.Start();//запускае сервер
            socket.Listen(0);//прослушиваем сокет                      
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
            thread.Start(client);//запускаем общение с клиентом при подключении
            clients.Add(client);//добавляем клиента в список
            //Console.WriteLine("Новое подключение.");
            socket.BeginAccept(AcceptCallBack, null);
        }

        private static void HandleClient(object o)
        {
            Client client = (Client)o;//наш клиен             
            BinaryReader reader = new BinaryReader(ms);//чтение из потока
            

            #region intoduce
            ms.Position = 0;            
            client.Socket.Receive(ms.GetBuffer());
            client.userName = reader.ReadString();
            client.machineName = reader.ReadString();
            client.currentDirectory = directory = reader.ReadString();
            #endregion
            
        }

        private static void threadServer()//поток сервера
        {
            Client client = new Client();   // создаем клиента  
            while (true)
            {
                if(clients.Count==0)//если клиенты не подключены то ожидаем их
                {                    
                    continue;
                }
                
                Console.Write(client.currentDirectory + ":> ");//выводим директорию клиента
                string request = Console.ReadLine();//ждем запрос
                CheckClients(ref client);//опрос клиентов на обнаружение их отключения
                if (request.Equals("cc") || client.isEmpity)//если была команда смены клиента или клиент пуст
                {
                    if (clients.Count != 0)//если список не пуст выводим список клиентов
                    {
                        Console.WriteLine("Клиенты:");
                        var count = 0;
                        foreach (var cl in clients)
                        {
                            Console.WriteLine(count + " => " + cl.userName + " IP => " + cl.Socket.RemoteEndPoint.ToString());
                            count++;
                        }
                        Console.Write("Введите номер клиента:> ");
                        try
                        {
                            int numb = Convert.ToInt32(Console.ReadLine());
                            if (numb > clients.Count - 1)
                            {
                                numb = clients.Count - 1;
                            }

                            client = clients[numb];
                        }
                        catch
                        {
                            Console.WriteLine("Ошибка ввода.");
                        }
                                                
                        
                    }
                    continue;
                }

                if (request.Equals("update"))//команда обнавления
                {
                    UpdateLibrary(client, ms);//обнавление библиотеки
                    continue;
                }

                ms.Position = 0;//выставляем позицию в буфере
                writer.Write(request);//записываем запрос
                client.Socket.Send(ms.GetBuffer());      //отсылаем запрос клиенту   
                try
                {
                    client.Socket.Receive(ms.GetBuffer()); //получаем ответ от клиента
                    ms.Position = 0;//выставляем позицию в буфере
                    
                }
                catch
                {
                    Console.WriteLine("Сбой связ.");
                    continue;
                }


                int code = reader.ReadInt32();//считываем код ответа
                switch (code)//выполняем действие согласно коду
                {
                    case 1: //получем файлы из дерриктории dir
                        GetDirectory(client, ms);
                        break;
                    case 2: //переходим в другую директорию cd
                        ChangeDirectory(client, ms);
                        break;
                    case 3: //переходим в другую директорию cd
                        Introduce(client, ms);
                        break;
                    case 4: //переходим в другую директорию cd
                        UploadFileAnswer(client);
                        break;
                    case 100: //переходим в другую директорию cd
                        CheckAnswer(client, ms);
                        break;
                    case 13: //переходим в другую директорию cd
                        ErroreCode(ms);
                        break;
                    default:
                        Console.WriteLine("Default code");
                        break;
                }
            }
        }

        private static void CheckAnswer(Client client, MemoryStream ms)//проверка версии
        {            
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            Console.WriteLine(reader.ReadString());
        }

        private static void UploadFileAnswer(Client client)//функция обработки ответа на загрузку
        {            
            Console.Write("Введите путь к файлу:> ");
            string pathF = Console.ReadLine();
            ms.Position = 0;
            Console.WriteLine(pathF);
            try
            {
                using (FileStream fs = new FileStream(pathF, FileMode.Open))
                {
                    byte[] data = new byte[fs.Length];//массив для файла
                    int lenghtfile = (int)fs.Length;//размер файла
                    fs.Read(data, 0, (int)fs.Length);//считываем фаил в массив

                    int seekF = 0;//ползунок
                    int size = 2000000;//размер кусков файла                        
                    string nameF = pathF.Substring(pathF.LastIndexOf('\\') + 1);//имя файла

                    writer.Write(lenghtfile);//передаем размер файла
                    writer.Write(nameF);//передаем имя
                    client.Socket.Send(ms.GetBuffer());//отсылаем 
                    client.Socket.Receive(ms.GetBuffer());//получаем ответ о готовности

                    ms.Position = 0;
                    string answer = reader.ReadString();
                    Console.WriteLine(answer);//выводим ответ

                    byte[] pocketData = new byte[size];//массив бай для файла 2мб
                    using (MemoryStream msF = new MemoryStream(new byte[lenghtfile], 0, lenghtfile, true, true))
                    {
                        BinaryWriter writerF = new BinaryWriter(msF);
                        fs.Read(data, 0, data.Length);
                        msF.Position = 0;                        
                        writerF.Write(data);
                        client.Socket.Send(msF.GetBuffer());
                        client.Socket.Receive(ms.GetBuffer());
                        Console.WriteLine(reader.ReadString());
                    }
                    #region old
                   /* using (MemoryStream msF = new MemoryStream(new byte[2000000], 0, 2000000, true, true))
                    {
                        
                        BinaryWriter writerF = new BinaryWriter(msF);//
                        bool end = false;

                        while (!end)//пока размер не равен
                        {
                            if(data.Length - (seekF + size) <= 0)
                            {
                                size = (seekF - data.Length)*(-1);
                                end = true;
                            }
                            
                            Array.Copy(data, seekF, pocketData, 0, size);

                            msF.Position = 0;
                            writerF.Write(pocketData);
                            client.Socket.Send(msF.GetBuffer());
                            ms.Position = 0;
                            client.Socket.Receive(ms.GetBuffer());
                            Console.WriteLine("Загружено " + reader.ReadInt32() + "%");
                            ms.Position = 0;
                            writer.Write(end);
                            client.Socket.Send(ms.GetBuffer());
                            seekF += size;

                        }
                        ms.Position = 0;
                        client.Socket.Receive(ms.GetBuffer());
                        Console.WriteLine(reader.ReadString());
                        

                    }*/
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        private static void UpdateLibrary(Client client, MemoryStream ms)//функция обнавления библиотеки
        {
            BinaryWriter writer = new BinaryWriter(ms);
            BinaryReader reader = new BinaryReader(ms);//чтение из поток

            try
            {
                using (FileStream fs = new FileStream(@"C:\Users\Иван\Documents\visual studio 2015\Projects\hclientlib\hclientlib\bin\Debug\hclientlib.dll", FileMode.Open))
                {
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    ms.Position = 0;
                    writer.Write("update");
                    writer.Write((int)fs.Length);
                   
                    writer.Write(data);
                    client.Socket.Send(ms.GetBuffer());      //отсылаем запрос клиенту                                                 
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                //Console.WriteLine("File ERROR "+ Environment.CurrentDirectory + "\\txt.txt");
            }
            
        }

        private static void Introduce(Client client, MemoryStream ms)//обработчик команды 
        {            
            BinaryReader reader = new BinaryReader(ms);           
            client.userName = reader.ReadString();
            client.machineName = reader.ReadString();
            client.currentDirectory = directory = reader.ReadString();
        }

        private static void ErroreCode(MemoryStream ms)
        {
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            Console.WriteLine(reader.ReadString());
        }

        private static void ChangeDirectory(Client client, MemoryStream ms)//смена директории
        {
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            client.currentDirectory = reader.ReadString();//записываем новую директорию
            Console.Clear();
            
        }

        private static void GetDirectory(Client client,MemoryStream ms)
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

        private static void CheckClients(ref Client currentClient)//функция проверки подключен ли клиент
        {
            for (int i = 0; i < clients.Count; i++)//проходит по списку клиентов и опрашивет их
            {
                if (clients[i].Socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0) //если он отключился
                {
                    
                    if (currentClient.Equals(clients[i]))  // проверяем не является ли он текущим клиентом
                    {
                        currentClient = new Client();//если да обнуляем его
                        Console.WriteLine("Клиент отключился");
                    }
                    clients.Remove(clients[i]);//удалям клиента из списка
                }           
            }
        }
        

    }

    class Client
    {
        public Socket Socket { get; set; }
        public string userName { get; set; }
        public string machineName { get; set; }
        public string currentDirectory { get; set; }
        public bool isEmpity { get; }
        public Client(Socket s)
        {
            isEmpity = false;
            Socket = s;
        }

        public Client()
        {
            isEmpity = true;
            userName = "null";
            machineName = "null";
            currentDirectory = "null";
                    
        }
    }
}
