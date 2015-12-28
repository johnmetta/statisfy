using System;
using System.Diagnostics;

namespace Statsify.Core.Model
{
    [DebuggerDisplay("{Name,nq}")]
    public class Metric
    {
        public string Name { get; private set; }

        public Series Series { get; private set; }

        public Metric(string name, Series series)
        {
            Name = name;
            Series = series;
        }

        /// <summary>
        /// Returns a new <see cref="Metric"/> by applying a <paramref name="transformation"/> to all <see cref="Datapoint"/> objects in <see cref="Series"/>.
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        public static Metric Transform(Metric metric, Func<double?, double?> transformation)
        {
            return new Metric(metric.Name, Series.Transform(metric.Series, transformation));
        }
    }
}