using System.Collections.Generic;
using System.Linq;

namespace Statsify.Agent.Impl
{
    public class Averager
    {
        private readonly IDictionary<string, Measurement> measurements = 
            new Dictionary<string, Measurement>();

        private class Measurement
        {
            public int Values { get; private set; }
            public double LastValue { get; private set; }
            public double AverageValue { get; private set; }

            public void Record(double value)
            {
                Values += 1;
                LastValue = value;
                AverageValue = AverageValue + (LastValue - AverageValue) / Values;
            }
        }

        public class Outlier
        {
            public string Name { get; private set; }
            public double AverageValue { get; private set; }
            public double LastValue { get; private set; }

            public Outlier(string name, double lastValue, double averageValue)
            {
                Name = name;
                LastValue = lastValue;
                AverageValue = averageValue;
            }
        }
        
        public IList<Outlier> GetOutliers(int minValues, double factor = 1)
        {
            var outliers = 
                measurements.
                    Where(m => m.Value.Values >= minValues).
                    Where(m => m.Value.LastValue > m.Value.AverageValue * factor).
                    Select(m => new Outlier(m.Key, m.Value.LastValue, m.Value.AverageValue)).
                    ToList();

            return outliers;
        }

        public void Record(string metric, double value)
        {
            Measurement measurement;
            if(!measurements.TryGetValue(metric, out measurement))
            {
                measurement = new Measurement();
                measurements.Add(metric, measurement);
            } // if

            measurement.Record(value);
        }
    }
}
