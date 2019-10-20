using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroSent.Models.Network
{
    //src: https://codereview.stackexchange.com/questions/151228/asynchronous-tcp-server
    public class NetworkServerSocket
    {
        private readonly int Port;
        private int MaxRespondeSize = 4096;


        public NetworkServerSocket(int port)
        {
            Port = port;
        }


        public Task<string> getSyntaxNetParseTree()
        {
            return Task<string>.Factory.StartNew(() =>
            {
                try
                {
                    TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
                    tcpListener.Start();

                    byte[] serverAnswere = new byte[MaxRespondeSize];
                    while (true)
                    {
                        using (TcpClient connectedTcpClient = tcpListener.AcceptTcpClient())
                        {
                            Console.WriteLine("Container connected");
                            using (NetworkStream stream = connectedTcpClient.GetStream())
                            {
                                int length;
                                while ((length = stream.Read(serverAnswere, 0, serverAnswere.Length)) != 0)
                                {
                                    byte[] incommingData = new byte[length];
                                    Array.Copy(serverAnswere, 0, incommingData, 0, length);
                                    string clientMessage = Encoding.ASCII.GetString(incommingData);
                                    return clientMessage;
                                }
                            }
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.StackTrace);
                }
                return null;
            });
        }
    }
}
