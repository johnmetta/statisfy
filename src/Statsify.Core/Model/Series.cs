using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Statsify.Core.Model
{
    public class Series
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public TimeSpan Interval { get; private set; }

        public ReadOnlyCollection<Datapoint> Datapoints { get; private set; }

        public Series(DateTime @from, DateTime until, TimeSpan interval, IEnumerable<Datapoint> datapoints)
        {
            Interval = interval;
            Until = until;
            From = @from;
            Datapoints = new ReadOnlyCollection<Datapoint>(new List<Datapoint>(datapoints));
        }

        public Series(Series series, IEnumerable<Datapoint> datapoints) :
            this(series.From, series.Until, series.Interval, datapoints)
        {
        }

        public Series Transform(Func<double?, double?> transformation)
        {
            return Transform(this, transformation);
        }

        /// <summary>
        /// Returns a new <see cref="Series"/> by applying a <paramref name="transformation"/> to all <see cref="Datapoint.Value"/> in <see cref="Datapoints"/> within <paramref name="series"/>.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        public static Series Transform(Series series, Func<double?, double?> transformation)
        {
            return new Series(series, Transform(series.Datapoints, transformation));
        }

        private static IEnumerable<Datapoint> Transform(IEnumerable<Datapoint> datapoints, Func<double?, double?> transformation)
        {
            foreach(var datapoint in datapoints)
            {
                var timestamp = datapoint.Timestamp;
                var value = transformation(datapoint.Value);

                yield return new Datapoint(timestamp, value);
            } // foreach
        }

    }
}