using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


//src: https://gist.github.com/danielbierwirth/0636650b005834204cb19ef5ae6ccedb
namespace MicroSent.Models.Network
{
    public class NetworkClientSocket
    {
        private readonly int Port;
        private int MaxRespondeSize = 4096;

        private const string HOST = "localhost";

        public NetworkClientSocket(int port)
        {
            this.Port = port;
        }

        public void sendStringToServer(string sentence)
        {
            TcpClient connection = new TcpClient(HOST, Port);
            byte[] serverAnswere = new byte[MaxRespondeSize];

            while (true)
            {
                try
                {
                    using (NetworkStream stream = connection.GetStream())
                    {
                        Console.WriteLine($"Connected to 127:0.0.1:{Port} (sending stream)");
                        sendMessageToServer(sentence, stream);

                        if(stream.Read(serverAnswere, 0, serverAnswere.Length) > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Server response: OK");
                            Console.ResetColor();
                            Console.WriteLine($"Closing connection to 127:0.0.1:{Port}");
                            break;
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No server response - will try again");
                        Console.ResetColor();
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Exception occured wile sending message:\n{e.StackTrace}");
                    Console.ResetColor();
                }
            }
        }

        public Task<string> receiveParseTree()
        {
            return Task<string>.Factory.StartNew(() =>
            {
                while (true)
                {
                    TcpClient connection = new TcpClient(HOST, Port);
                    byte[] serverAnswere = new byte[MaxRespondeSize];

                    try
                    {
                        using (NetworkStream stream = connection.GetStream())
                        {
                            int length = stream.Read(serverAnswere, 0, serverAnswere.Length);
                            if (length == 0)
                                continue;

                            Console.WriteLine($"Connected to 127:0.0.1:{Port} (receiving stream)");
                            
                            byte[] incommingData = new byte[length];
                            Array.Copy(serverAnswere, 0, incommingData, 0, length);
                            string clientMessage = Encoding.ASCII.GetString(incommingData);
                            
                            Console.WriteLine($"Closing connection to 127:0.0.1:{Port}");
                            return clientMessage;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Exception occured wile sending message:\n{e.StackTrace}");
                        Console.ResetColor();
                    }
                    return null;
                }
            });
        }

        private void sendMessageToServer(string message, NetworkStream stream)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            try
            {
                Console.WriteLine("Sending message to server");
                if (stream.CanWrite)
                {
                    stream.Write(byteMessage, 0, byteMessage.Length);
                }
                Console.WriteLine("Message successfully sent");
            }
            catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception occured wile sending message:\n{e.StackTrace}");
                Console.ResetColor();
            }
        }
    }
}
