using System;
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
            path = configuration.Storage.Path;                                
        }        

        public void Aggregate(string title, string message)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(path);
            annotationDatabase.WriteAnnotation(DateTime.UtcNow, title, message);
        }
    }
}
