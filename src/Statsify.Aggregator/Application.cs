using System;
using System.Linq;
using System.Reflection;

namespace Statsify.Aggregator
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
                        GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).
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