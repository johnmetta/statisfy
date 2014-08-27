using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Statsify.Core.Model;
using Statsify.Core.Storage;

namespace Statsify.Core.Components.Impl
{
    public class MetricRegistry : IMetricRegistry
    {
        private readonly string rootDirectory;

        public MetricRegistry(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public IEnumerable<string> ResolveMetricNames(string metricNameSelector)
        {
            return GetDatabaseFilePaths(metricNameSelector).
                Select(f => {
                    var directoryName = Path.GetDirectoryName(f.FullName);
                    Debug.Assert(directoryName != null, "directoryName != null");
                    directoryName = directoryName.Substring(rootDirectory.Length + 1);
                    
                    var fileName = Path.GetFileNameWithoutExtension(f.FullName);
                    
                    return Path.Combine(directoryName, fileName).Replace(Path.DirectorySeparatorChar, '.');
                });
        }

        public Metric ReadMetric(string metricName, DateTime @from, DateTime until, TimeSpan? precision = null)
        {
            var databaseFilePath = GetDatabaseFilePaths(metricName).FirstOrDefault();
            if(databaseFilePath == null) return null;

            var database = DatapointDatabase.Open(databaseFilePath.FullName);
            var series = database.ReadSeries(from, until, precision);

            return new Metric(metricName, series);
        }

        private IEnumerable<FileInfo> GetDatabaseFilePaths(string metricNameSelector)
        {
            var fragments = metricNameSelector.Split('.');
            return GetDatabaseFilePaths(new DirectoryInfo(rootDirectory), fragments, 0);
        }

        private IEnumerable<FileInfo> GetDatabaseFilePaths(DirectoryInfo directory, string[] fragments, int i)
        {
            if(i == fragments.Length - 1)
            {
                var files = directory.GetFiles(fragments[i] + ".db");
                foreach(var file in files)
                    yield return file;
            } // if
            else
            {
                foreach(var subdirectory in directory.GetDirectories(fragments[i]))
                {
                    directory = new DirectoryInfo(Path.Combine(directory.FullName, subdirectory.Name));
                    foreach(var metricName in GetDatabaseFilePaths(directory, fragments, i + 1))
                        yield return metricName;
                } // foreach
            } // else
        }
    }
}
