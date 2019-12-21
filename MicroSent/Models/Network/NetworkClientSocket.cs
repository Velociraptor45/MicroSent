using MicroSent.Models.Util;
using System;
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

        private readonly string Host;

        public NetworkClientSocket(int port, string host)
        {
            this.Port = port;
            this.Host = host;
        }

        public void sendStringToServer(string sentence)
        {
            TcpClient connection;
            try
            {
                connection = new TcpClient(Host, Port);
            }
            catch(SocketException e)
            {
                ConsolePrinter.printServerConnectionFailed(Host, Port, e);
                return;
            }

            byte[] serverAnswere = new byte[MaxRespondeSize];

            while (true)
            {
                using (NetworkStream stream = connection.GetStream())
                {
                    ConsolePrinter.printConnectionEstablished(Host, Port, true);
                    sendMessageToServer(sentence, stream);

                    if(stream.Read(serverAnswere, 0, serverAnswere.Length) > 0)
                    {
                        ConsolePrinter.printServerResponseOK();
                        ConsolePrinter.printConnectionClosed(Host, Port);
                        break;
                    }

                    ConsolePrinter.printNoServerResponse();
                    continue;
                }
            }
        }

        public Task<string> receiveParseTree()
        {
            return Task<string>.Factory.StartNew(() =>
            {
                while (true)
                {
                    TcpClient connection;
                    try
                    {
                        connection = new TcpClient(Host, Port);
                    }
                    catch (SocketException e)
                    {
                        ConsolePrinter.printServerConnectionFailed(Host, Port, e);
                        return null;
                    }
                    byte[] serverAnswere = new byte[MaxRespondeSize];

                    using (NetworkStream stream = connection.GetStream())
                    {
                        int length = stream.Read(serverAnswere, 0, serverAnswere.Length);
                        if (length == 0)
                            continue;

                        ConsolePrinter.printConnectionEstablished(Host, Port, false);

                        byte[] incommingData = new byte[length];
                        Array.Copy(serverAnswere, 0, incommingData, 0, length);
                        string clientMessage = Encoding.ASCII.GetString(incommingData);

                        ConsolePrinter.printConnectionClosed(Host, Port);
                        return clientMessage;
                    }
                }
            });
        }

        private void sendMessageToServer(string message, NetworkStream stream)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            try
            {
                ConsolePrinter.printSendingMessage(Host, Port);
                if (stream.CanWrite)
                {
                    Console.WriteLine($"SENDING: {message}");
                    stream.Write(byteMessage, 0, byteMessage.Length);
                }
                ConsolePrinter.printMessageSuccessfullySent(Host, Port);
            }
            catch(Exception e)
            {
                ConsolePrinter.printMessageSendingFailed(Host, Port, e);
            }
        }
    }
}
