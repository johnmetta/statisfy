namespace Statsify.Web.Api.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Configuration;
    using Models;

    internal class MetricService : IMetricService
    {
        private readonly DirectoryInfo rootDirectory;

        public MetricService(string rootDirectory)
        {
            this.rootDirectory = new DirectoryInfo(rootDirectory);
        }

        public MetricInfo[] Find(string query)
        {
            if (String.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(query);

            query = query.Trim();

            var nodes = query.Split('.');

            var endIndex = nodes.Length - 1;

            var infos = new List<FileSystemInfo>();

            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];

                infos = i == 0 ? GetRootFileSystemInfos(node, rootDirectory) : GetFileSystemInfos(infos, node, i == endIndex);

            }

            return infos.Select(info => new MetricInfo
            {
                IsLeaf = (info as FileInfo) != null,
                Path = GetMetricPath(info, rootDirectory),
                Name = GetMetricName(info),
                Info = info
            }).ToArray();
        }

        private static List<FileSystemInfo> GetFileSystemInfos(IEnumerable<FileSystemInfo> infos, string node, bool end)
        {
            var directories = infos.OfType<DirectoryInfo>().ToArray();

            if (end)
            {
                var namePattern = String.Format("{0}.db", node);                
                var files = directories.SelectMany(x => x.GetFiles(namePattern)).Cast<FileSystemInfo>().ToList();

                files.AddRange(directories.SelectMany(d => d.GetDirectories(node)));

                return files;
            }


            return directories.SelectMany(d => d.GetDirectories(node)).Cast<FileSystemInfo>().ToList();
        }

        private static List<FileSystemInfo> GetRootFileSystemInfos(string node, DirectoryInfo rootDirectory)
        {
            return rootDirectory.GetDirectories(node).Cast<FileSystemInfo>().ToList();
        }

        private static string GetMetricName(FileSystemInfo info)
        {
            return (info as FileInfo) != null ? Path.GetFileNameWithoutExtension(info.FullName) : info.Name;
        }

        private static string GetMetricPath(FileSystemInfo info, DirectoryInfo rootDirectory)
        {
            if (rootDirectory == null) throw new ArgumentNullException("rootDirectory");

            if (info == null)
                return null;            

            var directory = info as DirectoryInfo;
            var file = info as FileInfo;

            var sb = new StringBuilder();

            sb.AppendFormat(".{0}", file != null ? Path.GetFileNameWithoutExtension(info.FullName) : info.Name);

            directory = file != null ? file.Directory : directory.Parent;

            while (directory != null && directory.FullName != rootDirectory.FullName)
            {
                sb.Insert(0, String.Format(".{0}", directory.Name));

                directory = directory.Parent;
            }

            return sb.ToString(1, sb.Length - 1);
        }
    }
}
