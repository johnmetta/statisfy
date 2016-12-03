using System;

namespace Statsify.Client
{
    public interface IStatsifyChannel : IDisposable
    {
        void WriteBuffer(byte[] buffer);
    }
}