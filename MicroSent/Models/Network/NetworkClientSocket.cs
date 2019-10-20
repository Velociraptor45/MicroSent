using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


//src: https://gist.github.com/danielbierwirth/0636650b005834204cb19ef5ae6ccedb
namespace MicroSent.Models.Network
{
    public class NetworkClientSocket
    {
        private readonly int Port;

        private int MaxRespondeSize = 4096;

        public NetworkClientSocket(int port)
        {
            this.Port = port;
        }

        public async Task<List<string>> getParseTreeFromServer(string sentence)
        {
            TcpClient connection = new TcpClient("localhost", Port);
            Byte[] serverAnswere = new Byte[MaxRespondeSize];

            try
            {
                while (true)
                {
                    using (NetworkStream stream = connection.GetStream())
                    {
                        Console.WriteLine("Connected to stream");

                        sendMessageToServer(sentence, stream);

                        while (true)
                        {
                            int length;
                            while ((length = stream.Read(serverAnswere, 0, serverAnswere.Length)) != 0)
                            {
                                byte[] cleanData = new byte[length];
                                Array.Copy(serverAnswere, 0, cleanData, 0, length);

                                string response = Encoding.UTF8.GetString(cleanData);
                                return new List<string>() { response };
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception occured wile sending message:\n{e.StackTrace}");
                Console.ResetColor();
            }
            return new List<string>();
        }

        private void sendMessageToServer(string message, NetworkStream stream)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            try
            {
                Console.WriteLine("Sending message");
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
