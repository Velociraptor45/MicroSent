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

        private const string HOST = "localhost";

        public NetworkClientSocket(int port)
        {
            this.Port = port;
        }

        public void sendStringToServer(string sentence)
        {
            TcpClient connection;
            try
            {
                connection = new TcpClient(HOST, Port);
            }
            catch(SocketException e)
            {
                ConsolePrinter.printServerConnectionFailed(HOST, Port, e);
                return;
            }

            byte[] serverAnswere = new byte[MaxRespondeSize];

            while (true)
            {
                using (NetworkStream stream = connection.GetStream())
                {
                    ConsolePrinter.printConnectionEstablished(HOST, Port, true);
                    sendMessageToServer(sentence, stream);

                    if(stream.Read(serverAnswere, 0, serverAnswere.Length) > 0)
                    {
                        ConsolePrinter.printServerResponseOK();
                        ConsolePrinter.printConnectionClosed(HOST, Port);
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
                        connection = new TcpClient(HOST, Port);
                    }
                    catch (SocketException e)
                    {
                        ConsolePrinter.printServerConnectionFailed(HOST, Port, e);
                        return null;
                    }
                    byte[] serverAnswere = new byte[MaxRespondeSize];

                    using (NetworkStream stream = connection.GetStream())
                    {
                        int length = stream.Read(serverAnswere, 0, serverAnswere.Length);
                        if (length == 0)
                            continue;

                        ConsolePrinter.printConnectionEstablished(HOST, Port, false);

                        byte[] incommingData = new byte[length];
                        Array.Copy(serverAnswere, 0, incommingData, 0, length);
                        string clientMessage = Encoding.ASCII.GetString(incommingData);

                        ConsolePrinter.printConnectionClosed(HOST, Port);
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
                ConsolePrinter.printSendingMessage(HOST, Port);
                if (stream.CanWrite)
                {
                    stream.Write(byteMessage, 0, byteMessage.Length);
                }
                ConsolePrinter.printMessageSuccessfullySent(HOST, Port);
            }
            catch(Exception e)
            {
                ConsolePrinter.printMessageSendingFailed(HOST, Port, e);
            }
        }
    }
}
