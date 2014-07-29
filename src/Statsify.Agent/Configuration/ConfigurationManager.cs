using System;
using System.IO;
using System.Xml;

namespace Statsify.Agent.Configuration
{
    public class ConfigurationManager
    {
        private static readonly string Path;

        static ConfigurationManager()
        {
            Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Statsify.Agent", "statsify-agent.config");

#if DEBUG
            Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify-agent.config");
#endif

        }

        public StatsifyAgentConfigurationSection Configuration { get; private set; }

        public ConfigurationManager() :
            this(Path) { }

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
