using System.IO;

namespace Statsify.Aggregator.Api.Models
{
    public sealed class MetricInfo
    {
        public string Path { get; set; }

        public string Name { get; set; }

        public bool IsLeaf { get; set; }
        
        internal FileSystemInfo Info { get; set; }
    }
}
