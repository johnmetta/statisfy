using Statsify.Core.Storage;

namespace Statsify.Aggregator.ComponentModel
{
    public interface IDatapointDatabaseResolver
    {
        DatapointDatabase ResolveDatapointDatabase(string metric);
    }
}