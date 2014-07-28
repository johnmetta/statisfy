using System;
using System.IO;
using System.Xml;

namespace Statsify.Agent.Configuration
{
    public class ConfigurationManager
    {
        public StatsifyAgentConfigurationSection Configuration { get; private set; }

        public ConfigurationManager() :
            this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-agent.config")){}

        public ConfigurationManager(string configurationFilePath)
        {
            using(var stream = File.OpenRead(configurationFilePath))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(stream);

                if(xmlDocument.DocumentElement == null) return;

                using(var xmlReader = new XmlNodeReader(xmlDocument.DocumentElement))
                {
                    var configuration = new StatsifyAgentConfigurationSection(xmlReader);
                    Configuration = configuration;
                }
            }
        }
    }
}
