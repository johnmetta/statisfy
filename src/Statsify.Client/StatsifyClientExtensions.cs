using System;
using System.Diagnostics;

namespace Statsify.Client
{
    /// <summary>
    /// 
    /// </summary>
    public static class StatsifyClientExtensions
    {
        /// <summary>
        /// Increment a counter <paramref name="metric"/> by <c>1</c>.
        /// </summary>
        /// <param name="statsifyClient"></param>
        /// <param name="metric"></param>
        /// <param name="sample"></param>
        public static void Increment(this IStatsifyClient statsifyClient, string metric, double sample = 1)
        {
            statsifyClient.Counter(metric, 1, sample);
        }
        
        /// <summary>
        /// Decrement a counter <paramref name="metric"/> by <c>1</c>.
        /// </summary>
        /// <param name="statsifyClient"></param>
        /// <param name="metric"></param>
        /// <param name="sample"></param>
        public static void Decrement(this IStatsifyClient statsifyClient, string metric, double sample = 1)
        {
            statsifyClient.Counter(metric, -1, sample);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// A Timer is a measure of the number of milliseconds elapsed between a start and end time, for example the time to complete rendering of a web page for a user.
        /// </remarks>
        /// <param name="statsifyClient"></param>
        /// <param name="metric"></param>
        /// <param name="action"></param>
        /// <param name="sample"></param>
        public static void Time(this IStatsifyClient statsifyClient, string metric, Action action, double sample = 1)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            var value = stopwatch.ElapsedMilliseconds;

            statsifyClient.Time(metric, value, sample);
        }
    }
}