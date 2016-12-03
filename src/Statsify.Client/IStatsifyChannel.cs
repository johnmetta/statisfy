using System;

namespace Statsify.Client
{
    public interface IStatsifyChannel : IDisposable
    {
        bool SupportsBatchedWrites { get; }

        void WriteBuffer(byte[] buffer);
    }
}