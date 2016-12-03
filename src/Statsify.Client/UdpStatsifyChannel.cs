using System;
using System.Net;
using System.Net.Sockets;

namespace Statsify.Client
{
    internal class UdpStatsifyChannel : IStatsifyChannel
    {
        internal const int DefaultUdpPort = 8125;
        internal const string DefaultUdpHost = "127.0.0.1";

        private readonly UdpClient udpClient;

        public UdpStatsifyChannel(Uri uri)
        {
            udpClient = CreateUdpClient(uri);
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

        private static UdpClient CreateUdpClient(Uri uri)
        {
            var endpoint = ParseEndpoint(uri, DefaultUdpHost, DefaultUdpPort);
            var udpClient = new UdpClient(endpoint);
            
            return udpClient;
        }

        internal static IPEndPoint ParseEndpoint(Uri uri, string defaultHost, int defaultPort)
        {
            var host = uri.Host;
            var port = uri.Port;

            var ipAddress = 
                string.IsNullOrWhiteSpace(host) ? 
                    GetHostAddress(defaultHost) : 
                    GetHostAddress(host);

            if(port == -1)
                port = defaultPort;

            var endpoint = new IPEndPoint(ipAddress, port);
            return endpoint;
        }

        private static IPAddress GetHostAddress(string host)
        {
            var hostAddresses = Dns.GetHostAddresses(host);
            var hostAddress = hostAddresses[0];

            return hostAddress;
        }
    }
}