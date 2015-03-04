using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Statsify.Agent.Configuration
{
    public class ConfigurationManager
    {
        public StatsifyAgentConfigurationSection Configuration { get; private set; }

        public static ConfigurationManager Instance
        {
            get
            {
                var configurationFilePaths = new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Statsify", "Agent", "statsify-agent.config"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-agent.config")
                };

                var configurationFilePath = configurationFilePaths.FirstOrDefault(File.Exists);
                if(configurationFilePath == null) throw new ApplicationException();

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
                    var configuration = new StatsifyAgentConfigurationSection(xmlReader);
                    Configuration = configuration;
                }
            }
        }
    }
}
