using System;
using System.Net.Sockets;

namespace Statsify.Client
{
    internal class UdpStatsifyChannel : IStatsifyChannel
    {
        private readonly UdpClient udpClient;

        public UdpStatsifyChannel(Uri uri)
        {
            var host = uri.Host;
            var port = uri.Port;

            udpClient = new UdpClient(host, port);
        }

        public void WriteBuffer(byte[] buffer)
        {
            udpClient.Send(buffer, buffer.Length);
        }

        public void Dispose()
        {
            if(udpClient != null)
                ((IDisposable)udpClient).Dispose();
        }
    }
}