namespace Statsify.Web.Api.Configuration
{
    using System;
    using System.IO;
    using System.Xml;

    internal sealed class ConfigurationManager
    {
        public StatsifyConfigurationSection Configuration { get; private set; }

        public ConfigurationManager() :
            this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statsify.config")){}

        public ConfigurationManager(string configurationFilePath)
        {
            using (var stream = File.OpenRead(configurationFilePath))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(stream);

                if(xmlDocument.DocumentElement == null) return;

                using (var xmlReader = new XmlNodeReader(xmlDocument.DocumentElement))
                {
                    var configuration = new StatsifyConfigurationSection(xmlReader);
                    Configuration = configuration;
                } 
            }
        }
    }
}
