using System;

namespace Statsify.Client
{
    public interface IStatsifyClient : IDisposable
    {
        void Increment(string metric, double sample = 1);

        void Decrement(string metric, double sample = 1);

        void Counter(string metric, double value, double sample = 1);

        void Gauge(string metric, double value, double sample = 1);

        void GaugeDiff(string metric, double value, double sample = 1);

        void Time(string metric, double value, double sample = 1);

        void Time(string metric, Action action, double sample = 1);
    }
}