using System;
using System.Linq;
using System.Reflection;

namespace Statsify.Agent
{
    public static class Application
    {
        public static string Version
        {
            get
            {
                var assembly = typeof(Application).Assembly;
                var assemblyInformationalVersion =
                    assembly.
                        GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).
                        OfType<AssemblyInformationalVersionAttribute>().
                        FirstOrDefault();

                if(assemblyInformationalVersion == null)
                    return new Version(0, 0, 0).ToString(3);

                var version = assemblyInformationalVersion.InformationalVersion;
                return version;
            }
        }
    }
}