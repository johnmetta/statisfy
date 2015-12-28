using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NLog;
using Statsify.Agent.Util;

namespace Statsify.Agent.Impl
{
    public class RefreshableMetricSource : IMetricSource
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private IList<IMetricDefinition> metricDefinitions;
        private DateTime lastRefreshedAt = DateTime.UtcNow;
        private bool refreshing;
        private readonly TimeSpan refreshEvery;
        private readonly Func<IEnumerable<IMetricDefinition>> refreshCallback;
        private readonly Func<DateTime> currentTimeProvider;
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public RefreshableMetricSource(IEnumerable<IMetricDefinition> metricDefinitions, TimeSpan refreshEvery, Func<IEnumerable<IMetricDefinition>> refreshCallback) :
            this(metricDefinitions, refreshEvery, refreshCallback, () => DateTime.UtcNow)
        {
        }

        internal RefreshableMetricSource(IEnumerable<IMetricDefinition> metricDefinitions, TimeSpan refreshEvery, Func<IEnumerable<IMetricDefinition>> refreshCallback, Func<DateTime> currentTimeProvider)
        {
            this.metricDefinitions = new List<IMetricDefinition>(metricDefinitions ?? Enumerable.Empty<IMetricDefinition>());
            this.refreshEvery = refreshEvery;
            this.refreshCallback = refreshCallback;
            this.currentTimeProvider = currentTimeProvider;
        }

        public IEnumerable<IMetricDefinition> GetMetricDefinitions()
        {
            IList<IMetricDefinition> definitions;
            
            using(readerWriterLock.AcquireReadLock())
                definitions = metricDefinitions.ToList();

            TryRefreshMetricDefinitions();

            return definitions;
        }

        public void InvalidateMetricDefinition(IMetricDefinition metricDefinition)
        {
            using(readerWriterLock.AcquireWriteLock())
                metricDefinitions.Remove(metricDefinition);
        }

        private void TryRefreshMetricDefinitions()
        {
            if(lastRefreshedAt + refreshEvery > currentTimeProvider()) return;

            using(readerWriterLock.AcquireUpgradeableReadLock())
            {
                if(refreshing) return;

                using(readerWriterLock.AcquireWriteLock())
                {
                    if(refreshing) return;
                    refreshing = true;
                } // using
            } // using

            log.Trace("attempting to refresh metric definitions");
            
            ThreadPool.QueueUserWorkItem(s =>
            {
                var stopwatch = Stopwatch.StartNew();
                var definitions = refreshCallback();

                using(readerWriterLock.AcquireWriteLock())
                {
                    metricDefinitions = new List<IMetricDefinition>(definitions ?? Enumerable.Empty<IMetricDefinition>());
                    lastRefreshedAt = currentTimeProvider();
                    refreshing = false;
                } // using

                stopwatch.Stop();

                log.Trace("completed refreshing metric definitions in {0}", stopwatch.Elapsed);
            });
        }
    }
}