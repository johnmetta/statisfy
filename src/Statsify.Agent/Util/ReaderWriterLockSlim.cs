using System;
using System.Threading;

namespace Statsify.Agent.Util
{
    internal static class ReaderWriterLockSlimExtensions
    {
        internal static IDisposable AcquireReadLock(this ReaderWriterLockSlim readerWriterLock)
        {
            readerWriterLock.EnterReadLock();
            return new ReaderWriterLockToken(readerWriterLock.ExitReadLock);
        }

        internal static IDisposable AcquireWriteLock(this ReaderWriterLockSlim readerWriterLock)
        {
            readerWriterLock.EnterWriteLock();
            return new ReaderWriterLockToken(readerWriterLock.ExitWriteLock);
        }

        internal static IDisposable AcquireUpgradeableReadLock(this ReaderWriterLockSlim readerWriterLock)
        {
            readerWriterLock.EnterUpgradeableReadLock();
            return new ReaderWriterLockToken(readerWriterLock.ExitUpgradeableReadLock);
        }

            internal sealed class ReaderWriterLockToken : IDisposable
        {
            private readonly Action releaseCallback;

            public ReaderWriterLockToken(Action releaseCallback)
            {
                this.releaseCallback = releaseCallback;
            }

            public void Dispose()
            {
                releaseCallback();
            }
        }
    }
}
