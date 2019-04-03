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
                    case 3: //предача информации от вновь подключенного клиента
                        Introduce(client, ms);
                        break;
                    case 4: //закачка фаил на машину клиента
                        UploadFileAnswer(client);
                        break;
                    case 5: //скачивание файла с машины клиента
                        LoadFile(client);
                        break;
                    case 6: //скачивание файла с машины клиента
                        Test(client);
                        break;
                    case 100: //проверка вкрсии клиента
                        CheckAnswer(client, ms);
                        break;
                    case 13: //обработка ошибки
                        ErroreCode(ms);
                        break;
                    default:
                        Console.WriteLine("Default code");
                        break;
                }
            }
        }

        private static void Test(Client client)//функция для тестирования
        {
            ms.Position = 0;
            writer.Write("TEST OK");
            client.Socket.Send(ms.GetBuffer());
            ms.Position = 0;
            client.Socket.Receive(ms.GetBuffer());
            Console.WriteLine(reader.ReadString());
            ms.Position = 0;
            writer.Write("GOT IT");
            client.Socket.Send(ms.GetBuffer());
            ms.Position = 0;
            client.Socket.Receive(ms.GetBuffer());
            Console.WriteLine(reader.ReadString());
        }

        private static void LoadFile(Client client)//загрузка файла
        {
            ms.Position = 0;
            writer.Write(609);
            client.Socket.Send(ms.GetBuffer());
            ms.Position = 0;
            client.Socket.Receive(ms.GetBuffer());
            int length = reader.ReadInt32();
            if (length == -1)
            {
                Console.WriteLine("Не верное имя файла.");
                return;
            }
            byte[] data = new byte[length];
            string name = reader.ReadString();
            Console.WriteLine(name + " -> " + length);
            Console.WriteLine("Загрузка начата.");//код операции                        
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\download\\" + client.userName + "_" + name;
            ms.Position = 0;
            writer.Write(609);
            client.Socket.Send(ms.GetBuffer());
            using (MemoryStream msFile = new MemoryStream(new byte[length], 0, length, true, true))
            {
                BinaryReader readerFile = new BinaryReader(msFile);
                msFile.Position = 0;
                client.Socket.Receive(msFile.GetBuffer());
                data = readerFile.ReadBytes(length);
                
                ms.Position = 0;
                writer.Write("end");
                client.Socket.Send(ms.GetBuffer());

            }
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                fs.Write(data, 0, data.Length);
            }
            ms.Position = 0;
            client.Socket.Receive(ms.GetBuffer());
            string answ = reader.ReadString();
            Console.WriteLine("otvet -> " + answ);
            Console.WriteLine(answ);
        }
        /* private static void LoadFile(Client client)//загрузка файла
         {
             ms.Position = 0;
             client.Socket.Receive(ms.GetBuffer());
             int length = reader.ReadInt32();           
             if (length == -1)
             {
                 Console.WriteLine("Не верное имя файла.");
                 return;
             }
             byte[] data = new byte[length];
             string name = reader.ReadString();
             //ms.Position = 0;
             //writer.Write("ok");
             //client.Socket.Send(ms.GetBuffer());
             Console.WriteLine("Загрузка начата.");//код операции            
             string path = Environment.CurrentDirectory + "\\download\\" + client.userName + "_" + name;
             using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
             {
                 using (MemoryStream msFile = new MemoryStream(new byte[length], 0, length, true, true))
                 {
                     BinaryReader readerFile = new BinaryReader(msFile);
                     msFile.Position = 0;
                     client.Socket.Receive(msFile.GetBuffer());                  
                     data = readerFile.ReadBytes(length);
                     fs.Write(data, 0, data.Length);                                                        
                     Thread.Sleep(5000);
                 }
                 ms.Position = 0;
                 client.Socket.Receive(ms.GetBuffer());               
                 //string answ = reader.ReadString();
                 Console.WriteLine("Done");
             }
         }*/

        private static void CheckAnswer(Client client, MemoryStream ms)//проверка версии
        {            
            BinaryReader reader = new BinaryReader(ms);//чтение из поток
            Console.WriteLine(reader.ReadString());
        }

        private static void UploadFileAnswer(Client client)//функция обработки ответа на загрузку
        {
            Console.Clear();
            Console.Write("Введите путь к файлу:> ");
            string pathF = Console.ReadLine();
            
            while(!File.Exists(pathF))
            {
                Console.Clear();
                Console.WriteLine("Ошибка в имени файла или фаил не существует.");
                Console.Write("Введите повторно путь к файлу или abort для прекращения операции:> ");
                pathF = Console.ReadLine();
                if(pathF.Equals("abort"))
                {
                    ms.Position = 0;
                    writer.Write(-1);//передаем имя
                    client.Socket.Send(ms.GetBuffer());//отсылаем
                    return;
                }
            }

            ms.Position = 0;
            try
            {
                using (FileStream fs = new FileStream(pathF, FileMode.Open))
                {
                    byte[] data = new byte[fs.Length];//массив для файла
                    int lenghtfile = (int)fs.Length;//размер файла
                    fs.Read(data, 0, (int)fs.Length);//считываем фаил в массив                                                    
                    string nameF = pathF.Substring(pathF.LastIndexOf('\\') + 1);//имя файла
                    writer.Write(lenghtfile);//передаем размер файла
                    writer.Write(nameF);//передаем имя
                    client.Socket.Send(ms.GetBuffer());//отсылаем 
                    client.Socket.Receive(ms.GetBuffer());//получаем ответ о готовности
                    ms.Position = 0;
                    string answer = reader.ReadString();
                    Console.WriteLine(answer);//выводим ответ

                    
                    using (MemoryStream msF = new MemoryStream(new byte[lenghtfile], 0, lenghtfile, true, true))
                    {
                        BinaryWriter writerF = new BinaryWriter(msF);
                        fs.Read(data, 0, data.Length);
                        msF.Position = 0;                        
                        writerF.Write(data);
                        client.Socket.Send(msF.GetBuffer());
                        ms.Position = 0;
                        client.Socket.Receive(ms.GetBuffer());
                        Console.WriteLine(reader.ReadString());
                    }
                   
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
                using (FileStream fs = new FileStream(@"C:\Users\Иван\Documents\visual studio 2015\Projects\HProject\HClient\HClient\bin\Release\HClient.exe", FileMode.Open))
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
