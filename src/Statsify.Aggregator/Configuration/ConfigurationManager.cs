using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace Statsify.Aggregator.Configuration
{
    public class ConfigurationManager
    {
        private const string ConfigurationFileName = "statsify-aggregator.config";

        public StatsifyAggregatorConfigurationSection Configuration { get; private set; }

        public static ConfigurationManager Instance
        {
            get
            {
                var programDataDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Statsify", "Aggregator");
                var programDataConfigurationFilePath = Path.Combine(programDataDirectoryPath, ConfigurationFileName);

                var configurationFilePaths = new[] {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationFileName),
                    programDataConfigurationFilePath
                };

                var configurationFilePath = configurationFilePaths.FirstOrDefault(File.Exists);
                if(configurationFilePath == null)
                {
                    if(!Directory.Exists(programDataDirectoryPath))
                        Directory.CreateDirectory(programDataDirectoryPath);

                    using(var stream = typeof(ConfigurationManager).Assembly.GetManifestResourceStream("Statsify.Aggregator." + ConfigurationFileName))
                    {
                        if(stream == null) throw new Exception(); // FIXME

                        using(var streamReader = new StreamReader(stream))
                        {
                            var configuration = streamReader.ReadToEnd();
                            configuration = configuration.Replace("{{ storage-path }}", 
                                Path.Combine(programDataDirectoryPath, "Data"));

                            using(var fileStream = File.Create(programDataConfigurationFilePath))
                            using(var streamWriter = new StreamWriter(fileStream))
                                streamWriter.Write(configuration);
                        } // using
                    } // using

                    configurationFilePath = programDataConfigurationFilePath;
                } // if


                return new ConfigurationManager(configurationFilePath);
            }
        }

        private ConfigurationManager(string configurationFilePath)
        {
            using(var stream = File.OpenRead(configurationFilePath))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(stream);

                if(xmlDocument.DocumentElement == null) return;

                using(var xmlReader = new XmlNodeReader(xmlDocument.DocumentElement))
                {
                    var configuration = new StatsifyAggregatorConfigurationSection(xmlReader);
                    Configuration = configuration;
                }
            }
        }
    }
}
