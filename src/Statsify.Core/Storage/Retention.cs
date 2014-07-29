using System;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{Precision}:{History}")]
    public class Retention
    {
        public Precision Precision { get; private set; }

        public History History { get; private set; }

        public Retention(TimeSpan precision, TimeSpan history)
        {
            Precision = new Precision(precision);

            History = new History(history, precision);
        }

        public Retention(TimeSpan precision, int history)
        {
            Precision = new Precision(precision);

            History = new History(TimeSpan.FromSeconds(Precision * history), precision);
        }
    }
}
