using System;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{Precision}:{History}")]
    public class Retention
    {
        public TimeSpan Precision { get; private set; }

        public TimeSpan History { get; private set; }

        public int SecondsPerPoint { get; private set; }

        public int Points { get; private set; }

        public Retention(TimeSpan precision, TimeSpan history)
        {
            Precision = precision;
            History = history;

            SecondsPerPoint = (int)precision.TotalSeconds;
            Points = (int)(history.TotalSeconds / precision.TotalSeconds);
        }
    }
}