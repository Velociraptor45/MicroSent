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

        public NetworkClientSocket(int port)
        {
            this.Port = port;
        }

        public void sendStringToServer(string sentence)
        {
            TcpClient connection = new TcpClient("localhost", Port);

            try
            {
                using (NetworkStream stream = connection.GetStream())
                {
                    Console.WriteLine("Connected to stream");
                    sendMessageToServer(sentence, stream);
                }
            }
            catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception occured wile sending message:\n{e.StackTrace}");
                Console.ResetColor();
            }
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
