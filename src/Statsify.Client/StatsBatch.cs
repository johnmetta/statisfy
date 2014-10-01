using System;
using System.Collections.Generic;

namespace Statsify.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class StatsBatch : IStatsifyClient
    {
        private readonly IList<Action<IStatsifyClient>> actions = new List<Action<IStatsifyClient>>();
 
        public void Counter(string metric, double value, double sample = 1)
        {
            actions.Add(sc => sc.Counter(metric, value, sample));
        }

        public void Gauge(string metric, double value, double sample = 1)
        {
            actions.Add(sc => sc.Gauge(metric, value, sample));
        }

        public void GaugeDiff(string metric, double value, double sample = 1)
        {
            actions.Add(sc => sc.GaugeDiff(metric, value, sample));
        }

        public void Time(string metric, double value, double sample = 1)
        {
            actions.Add(sc => sc.Time(metric, value, sample));
        }

        public void Annotation(string title, string message)
        {
            actions.Add(sc => sc.Annotation(title, message));
        }

        /// <summary>
        /// Publishes all batched operations to <paramref name="statsifyClient"/>.
        /// </summary>
        /// <param name="statsifyClient"></param>
        public void Publish(IStatsifyClient statsifyClient)
        {
            foreach(var action in actions)
                action(statsifyClient);

            actions.Clear();
        }
    }
}
