using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Statsify.Aggregator.Configuration
{
    public class ConfigurationManager
    {
        public StatsifyAggregatorConfigurationSection Configuration { get; private set; }

        private ConfigurationManager() :
            this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-aggregator.config")){}

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

        public static ConfigurationManager Instance
        {
            get
            {
                var configurationFilePaths = new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Statsify", "Aggregator", "statsify-aggregator.config"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-aggregator.config")
                };

                var configurationFilePath = configurationFilePaths.FirstOrDefault(File.Exists);
                if(configurationFilePath == null) throw new ApplicationException();

                return new ConfigurationManager(configurationFilePath);
            }
        }
    }
}
