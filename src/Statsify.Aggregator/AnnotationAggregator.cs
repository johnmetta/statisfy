using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LinqToDB;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;

namespace Statsify.Aggregator
{
    public class AnnotationAggregator
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly StatsifyAggregatorConfigurationSection configuration;

        public AnnotationAggregator(StatsifyAggregatorConfigurationSection configuration)
        {
            this.configuration = configuration;                                
        }        

        public void Aggregate(string message)
        {
            using(var dc = new AnnotationDataContext(configuration.Storage.Path))
            {
                var annotation = new Annotation { Date = DateTime.UtcNow, Message = message };

                dc.Annotations.Insert(() => annotation);

                log.Info("add annotation: '{0}'", annotation.Message);
            }            
        }
    }
}
