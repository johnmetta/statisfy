using System;

namespace Statsify.Aggregator.Network
{
    public class UdpDatagramEventArgs :EventArgs
    {
        public byte[] Buffer { get; private set; }

        public UdpDatagramEventArgs(byte[] buffer)
        {
            Buffer = buffer;
        }
    }
}