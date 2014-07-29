using System;

namespace Statsify.Client
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Documentation is from https://github.com/b/statsd_spec.
    /// </remarks>
    public interface IStatsifyClient : IDisposable
    {
        /// <summary>
        /// Increments counter <paramref name="metric"/> by <paramref name="value"/>.
        /// </summary>
        /// <remarks>A Counter is a Gauge calculated at the server.</remarks>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        /// <param name="sample"></param>
        void Counter(string metric, double value, double sample = 1);

        /// <summary>
        /// Records an arbitrary <paramref name="value"/> for gauge <paramref name="metric"/>.
        /// </summary>
        /// <remarks>
        /// A Gauge is an instantaneous measurement of a value, like the gas gauge in a car. It differs from a counter by being calculated at the client rather than the server.
        /// </remarks>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        /// <param name="sample"></param>
        void Gauge(string metric, double value, double sample = 1);

        /// <summary>
        /// Increments or decrements gauge <paramref name="metric"/> by <paramref name="value"/>.
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        /// <param name="sample"></param>
        void GaugeDiff(string metric, double value, double sample = 1);

        /// <summary>
        /// Records the measurement for timer <paramref name="metric"/>.
        /// </summary>
        /// <remarks>
        /// A Timer is a measure of the number of milliseconds elapsed between a start and end time, for example the time to complete rendering of a web page for a user.
        /// </remarks>
        /// <param name="metric"></param>
        /// <param name="value"></param>
        /// <param name="sample"></param>
        void Time(string metric, double value, double sample = 1);
    }
}