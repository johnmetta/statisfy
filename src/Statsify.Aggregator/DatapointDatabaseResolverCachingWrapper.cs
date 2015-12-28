using System.Collections.Concurrent;
using System.IO;
using Statsify.Aggregator.ComponentModel;
using Statsify.Core.Storage;

namespace Statsify.Aggregator
{
    public class DatapointDatabaseResolverCachingWrapper : IDatapointDatabaseResolver
    {
        private readonly ConcurrentDictionary<string, DatapointDatabase> databaseCache = new ConcurrentDictionary<string, DatapointDatabase>(); 
        private readonly IDatapointDatabaseResolver datapointDatabaseResolver;

        public DatapointDatabaseResolverCachingWrapper(IDatapointDatabaseResolver datapointDatabaseResolver)
        {
            this.datapointDatabaseResolver = datapointDatabaseResolver;
        }

        public DatapointDatabase ResolveDatapointDatabase(string metric)
        {
            var key = metric.ToLowerInvariant();

            DatapointDatabase datapointDatabase;
            if(databaseCache.TryGetValue(key, out datapointDatabase))
            {
                if(!File.Exists(datapointDatabase.Path))
                {
                    databaseCache.TryRemove(key, out datapointDatabase);
                    return null;
                } // if

                return datapointDatabase;
            } // if

            datapointDatabase = datapointDatabaseResolver.ResolveDatapointDatabase(metric);
            databaseCache.TryAdd(key, datapointDatabase);

            return datapointDatabase;
        }
    }
}