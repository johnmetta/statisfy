using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;

namespace Statsify.Aggregator
{
    public class DatapointDatabaseResolver : IDatapointDatabaseResolver
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly StatsifyAggregatorConfigurationSection configuration;

        public DatapointDatabaseResolver(StatsifyAggregatorConfigurationSection configuration)
        {
            this.configuration = configuration;
        }

        public DatapointDatabase ResolveDatapointDatabase(string metric)
        {
            var fullPath = Path.Combine(configuration.Storage.Path, metric.Replace('.', Path.DirectorySeparatorChar) + ".db");
            var directory = Path.GetDirectoryName(fullPath);
            if(directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var downsampling = configuration.Downsampling.FirstOrDefault(d => Regex.IsMatch(metric, d.Pattern));
            if(downsampling == null) return null;

            var storage = configuration.Storage.FirstOrDefault(a => Regex.IsMatch(metric, a.Pattern));
            if(storage == null) return null;

            log.Trace("creating Datapoint Database for Metric '{0}' using Downsampling settings '{1}' and Storage settings '{2}'", 
                metric, downsampling.Name, storage.Name);

            var retentonPolicy = new RetentionPolicy(storage.Retentions);
            var database = DatapointDatabase.OpenOrCreate(fullPath, downsampling.Factor, downsampling.Method, retentonPolicy);

            return database;
        }
    }
}