using System;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Timestamp
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly long ticks;

        // ReSharper disable once UnusedMember.Local
        private string DebuggerDisplay
        {
            get { return ((DateTime)this).ToString("HH:mm:ss dd.MM.yyyy"); }
        }

        public Timestamp(long ticks)
        {
            this.ticks = ticks;
        }

        public static implicit operator Timestamp(long ticks)
        {
            return new Timestamp(ticks);
        }

        public static implicit operator Timestamp(DateTime dateTime)
        {
            var elapsedTime = dateTime - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static implicit operator long(Timestamp timestamp)
        {
            return timestamp.ticks;
        }

        public static implicit operator DateTime(Timestamp timestamp)
        {
            return Epoch.AddSeconds(timestamp.ticks);
        }

        public static bool operator ==(Timestamp l, Timestamp r)
        {
            return l.ticks == r.ticks;
        }

        public static bool operator !=(Timestamp l, Timestamp r)
        {
            return l.ticks != r.ticks;
        }

        public static bool operator <(Timestamp l, Timestamp r)
        {
            return l.ticks < r.ticks;
        }

        public static bool operator >(Timestamp l, Timestamp r)
        {
            return l.ticks > r.ticks;
        }
    }
}
