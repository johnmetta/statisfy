using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using NLog;
using Statsify.Core.Model;
using Statsify.Core.Storage;

namespace Statsify.Core.Components.Impl
{
    public class MetricRegistry : IMetricRegistry
    {
        private static readonly ObjectCache MetricNamesCache = MemoryCache.Default;

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly string rootDirectory;

        public MetricRegistry(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public ISet<string> ResolveMetricNames(string metricNameSelector)
        {
            var key = string.Format("metric-names:{0}", metricNameSelector);

            var metricNames = MetricNamesCache.Get(key) as IList<string>;
            if(metricNames == null)
            {
                metricNames =
                    GetDatabaseFiles(metricNameSelector).
                    Select(f => {
                        var directoryName = Path.GetDirectoryName(f.FullName);
                        Debug.Assert(directoryName != null, "directoryName != null");
                        directoryName = directoryName.Substring(rootDirectory.Length + 1);
                    
                        var fileName = Path.GetFileNameWithoutExtension(f.FullName);
                    
                        return Path.Combine(directoryName, fileName).Replace(Path.DirectorySeparatorChar, '.');
                    }).
                    ToList();

                MetricNamesCache.Set(
                    new CacheItem(key, metricNames), 
                    new CacheItemPolicy {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10)
                    });
            } // if

            return new HashSet<string>(metricNames, StringComparer.InvariantCultureIgnoreCase);
        }

        public Metric ReadMetric(string metricName, DateTime @from, DateTime until, TimeSpan? precision = null)
        {
            var databaseFilePath = GetDatabaseFiles(metricName).FirstOrDefault();
            if(databaseFilePath == null) return null;

            var database = DatapointDatabase.Open(databaseFilePath.FullName);
            var series = database.ReadSeries(from, until, precision);

            return new Metric(metricName, series);
        }

        public void PurgeMetrics(DateTime lastUpdatedAt)
        {
            log.Info("started purging metrics older than '{0:O}'", lastUpdatedAt);

            var filePaths =
                Directory.
                    EnumerateFiles(rootDirectory, "*.db", SearchOption.AllDirectories).
                    Where(File.Exists).
                    Where(fp =>
                    {
                        try
                        {
                            var fileInfo = new FileInfo(fp);

                            if(fileInfo.LastWriteTimeUtc < lastUpdatedAt) return true;
                            if(fileInfo.Length < 1024 * 16) return true;

                            return false;
                        }
                        catch(Exception e)
                        {
                            return false;
                        }
                    });

            foreach(var filePath in filePaths)
            {
                try
                {
                    File.Delete(filePath);
                    log.Info("deleted '{0}'", filePath);
                } // try
                catch(Exception e)
                {
                    log.Error(e, "could not delete '{0}'", filePath);
                } // catch
            } // foreach

            log.Info("completed purging metrics older than '{0:O}'", lastUpdatedAt);
        }

        private IEnumerable<FileInfo> GetDatabaseFiles(string metricNameSelector)
        {
            var fragments = metricNameSelector.Split('.');
            return GetDatabaseFiles(new DirectoryInfo(rootDirectory), fragments, 0);
        }

        private IEnumerable<FileInfo> GetDatabaseFiles(DirectoryInfo directoryInfo, string[] fragments, int i)
        {
            var fragment = fragments[i];

            if(i == fragments.Length - 1)
            {
                if(fragment.StartsWith("{") && fragment.EndsWith("}"))
                {
                    fragment = fragment.TrimStart('{').TrimEnd('}');
                    
                    var subfragments = fragment.Split(',');
                    foreach(var file in subfragments.SelectMany(subfragment => GetDatabaseFiles(directoryInfo, subfragment)))
                        yield return file;
                }
                else
                {
                    foreach(var file in GetDatabaseFiles(directoryInfo, fragment))
                        yield return file;
                } // else
            } // if
            else
            {
                if(fragment.StartsWith("{") && fragment.EndsWith("}"))
                {
                    fragment = fragment.TrimStart('{').TrimEnd('}');
                    
                    var subfragments = fragment.Split(',');
                    foreach(var file in subfragments.SelectMany(subfragment => GetDatabaseFiles(directoryInfo, subfragment, fragments, i)))
                        yield return file;
                } // if
                else
                {
                    foreach(var file in GetDatabaseFiles(directoryInfo, fragment, fragments, i))
                        yield return file;
                } // else
            } // else
        }

        private IEnumerable<FileInfo> GetDatabaseFiles(DirectoryInfo directoryInfo, string searchPattern)
        {
            var files = directoryInfo.GetFiles(searchPattern + ".db");
            foreach(var file in files)
                yield return file;
        } 

        private IEnumerable<FileInfo> GetDatabaseFiles(DirectoryInfo directoryInfo, string searchPatten, string[] fragments, int i)
        {
            foreach(var subdirectory in directoryInfo.GetDirectories(searchPatten))
            {
                var subdirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, subdirectory.Name));
                foreach(var metricName in GetDatabaseFiles(subdirectoryInfo, fragments, i + 1))
                    yield return metricName;
            } // foreach
        }
    }
}
