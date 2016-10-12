using System;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>
    {
        public static Func<Timestamp, string> DebuggerDisplayFormatter =
            t => ((DateTime)t).ToString("HH:mm:ss dd.MM.yyyy");

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly long ticks;

        // ReSharper disable once UnusedMember.Local
        private string DebuggerDisplay
        {
            get { return DebuggerDisplayFormatter(this); }
        }

        public Timestamp(long ticks)
        {
            this.ticks = ticks;
        }

        public int CompareTo(Timestamp other)
        {
            if(this == other) return 0;
            return this < other ? -1 : 1;
        }

        public override string ToString()
        {
            return DebuggerDisplay;
        }

        public bool Equals(Timestamp other)
        {
            return ticks == other.ticks;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            return obj is Timestamp && Equals((Timestamp) obj);
        }

        public override int GetHashCode()
        {
            return ticks.GetHashCode();
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

        public Timestamp RoundDownModulo(int modulo)
        {
            var roundedTicks = (ticks - (ticks % modulo));
            return new Timestamp(roundedTicks);
        }

        public Timestamp RoundUpModulo(int modulo)
        {
            var roundedTicks = (ticks - (ticks % modulo)) + modulo;
            return new Timestamp(roundedTicks);
        }
    }
}
