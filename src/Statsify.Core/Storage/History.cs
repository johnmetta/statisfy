using System;

namespace Statsify.Core.Storage
{
    public struct History
    {
        private readonly TimeSpan history;
        private readonly TimeSpan precision;

        public History(TimeSpan history, TimeSpan precision)
        {
            this.history = history;
            this.precision = precision;
        }

        public static implicit operator TimeSpan(History history)
        {
            return history.history;
        }

        public static implicit operator int(History history)
        {
            return (int)(history.history.TotalSeconds / history.precision.TotalSeconds);
        }

        public override string ToString()
        {
            return history.ToString();
        }
    }
}