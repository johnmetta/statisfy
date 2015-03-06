using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Statsify.Agent.Configuration
{
    public class ConfigurationManager
    {
        private const string ConfigurationFileName = "statsify-agent.config";

        public StatsifyAgentConfigurationSection Configuration { get; private set; }

        public static ConfigurationManager Instance
        {
            get
            {
                var programDataDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Statsify", "Agent");
                var programDataConfigurationFilePath = Path.Combine(programDataDirectoryPath, ConfigurationFileName);
                
                var configurationFilePaths = new[] {
                    programDataConfigurationFilePath,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationFileName)
                };

                var configurationFilePath = configurationFilePaths.FirstOrDefault(File.Exists);
                if(configurationFilePath == null)
                {
                    if(!Directory.Exists(programDataDirectoryPath))
                        Directory.CreateDirectory(programDataDirectoryPath);

                    using(var stream = typeof(ConfigurationManager).Assembly.GetManifestResourceStream("Statsify.Agent." + ConfigurationFileName))
                    using(var fileStream = File.Create(programDataConfigurationFilePath))
                    {
                        if(stream != null) 
                            stream.CopyTo(fileStream);
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
                    var configuration = new StatsifyAgentConfigurationSection(xmlReader);
                    Configuration = configuration;
                }
            }
        }
    }
}
