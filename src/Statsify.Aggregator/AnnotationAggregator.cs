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

        private readonly ManualResetEvent stopEvent;

        private readonly IList<Tuple<DateTime, string>> annotations = new List<Tuple<DateTime, string>>();

        private readonly ConcurrentQueue<Tuple<DateTime, string>> flushQueue = new ConcurrentQueue<Tuple<DateTime, string>>();

        private readonly object sync = new object();

        public AnnotationAggregator(StatsifyAggregatorConfigurationSection configuration, ManualResetEvent stopEvent)
        {
            this.configuration = configuration;

            this.stopEvent = stopEvent;

            var flushThread = new Thread(FlushCallback);

            flushThread.Start();            
        }

        private void FlushCallback()
        {
            if(!AnnotationDataContext.Exists(configuration.Storage.Path))
            {
                AnnotationDataContext.CreateDatabase(configuration.Storage.Path);

                log.Info("creating annotation database");
            }

            while(!stopEvent.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                var n = 0;

                var count = flushQueue.Count;

                log.Trace("started flushing {0} annotations", count);

                var sw = Stopwatch.StartNew();


                Tuple<DateTime, string> datapoint;

                while(flushQueue.TryDequeue(out datapoint))
                {
                    using(var dc = new AnnotationDataContext(configuration.Storage.Path))
                    {
                        n++;

                        dc.Annotations.Insert(() => new Annotation { Date = datapoint.Item1, Message = datapoint.Item2 });
                    }
                }

                log.Trace("completed flushing {0} annotations in {1} ({2:N2} per second); {3} items in backlog queue", n,
                    sw.Elapsed, count / sw.Elapsed.TotalSeconds, flushQueue.Count);

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public void Aggregate(string annotation)
        {            
            lock (sync)
            {
                annotations.Add(new Tuple<DateTime, string>(DateTime.UtcNow, annotation));
            }
        }

        public void Flush()
        {
            lock(sync)
            {   
                foreach (var annotation in annotations.ToArray())
                {
                    flushQueue.Enqueue(Tuple.Create(annotation.Item1, annotation.Item2));

                    annotations.Remove(annotation);
                }
            }
        }

    }
}
