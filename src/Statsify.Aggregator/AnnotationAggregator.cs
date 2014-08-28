using System;
using System.IO;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;

namespace Statsify.Aggregator
{
    public class AnnotationAggregator
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly string path;

        public AnnotationAggregator(StatsifyAggregatorConfigurationSection configuration)
        {
            path = Path.Combine(configuration.Storage.Path, "annotations.db");                                
        }        

        public void Aggregate(string title, string message)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(path);
            annotationDatabase.WriteAnnotation(DateTime.UtcNow, title, message);
        }
    }
}
