using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Statsify.Aggregator.Configuration
{
    public class ConfigurationManager
    {
        public StatsifyAggregatorConfigurationSection Configuration { get; private set; }

        public ConfigurationManager() :
            this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-aggregator.config"))
        {
        }

        public ConfigurationManager(string configurationFilePath)
        {
            using(var stream = File.OpenRead(configurationFilePath))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(stream);

                using(var xmlReader = new XmlNodeReader(xmlDocument.DocumentElement))
                {
                    var configuration = new StatsifyAggregatorConfigurationSection(xmlReader);
                    Configuration = configuration;
                } // using
            } // using
        }
    }
}
