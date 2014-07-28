using System;
using System.Net;
using System.Net.Sockets;

namespace Statsify.Aggregator.Network
{
    public class UdpDatagramReader : IDisposable
    {
        private IPEndPoint ipEndpoint;
        private UdpClient udpClient;
        
        public event UdpDatagramHandler DatagramHandler;

        public UdpDatagramReader(IPAddress ipAddress, int port) :
            this(new IPEndPoint(ipAddress, port)){}

        public UdpDatagramReader(IPEndPoint ipEndpoint)
        {
            this.ipEndpoint = ipEndpoint;

            udpClient = new UdpClient(ipEndpoint);

            udpClient.BeginReceive(UdpClientBeginReceiveCallback, null);
        }

        private void UdpClientBeginReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if(udpClient != null) 
                {
                    var buffer = udpClient.EndReceive(ar, ref ipEndpoint);

                    if(DatagramHandler != null)
                        DatagramHandler(this, new UdpDatagramEventArgs(buffer));
                }

                if(udpClient != null)
                    udpClient.BeginReceive(UdpClientBeginReceiveCallback, null);
            }
            catch(ObjectDisposedException e)
            {
                Console.WriteLine(e);
            }
        }

        public void Dispose()
        {
            if(udpClient == null) return;
            
            ((IDisposable)udpClient).Dispose();

            udpClient = null;
        }
    }
}
