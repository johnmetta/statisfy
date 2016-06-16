using System;
using System.Linq;
using System.Reflection;

namespace Statsify.Aggregator
{
    public static class Application
    {
        public static Version Version
        {
            get
            {
                var assemblyFileVersion =
                    typeof(Application).Assembly.
                        GetCustomAttributes(typeof(AssemblyFileVersionAttribute)).
                        OfType<AssemblyFileVersionAttribute>().
                        FirstOrDefault();
                
                Version version;
                if(assemblyFileVersion != null && Version.TryParse(assemblyFileVersion.Version, out version))
                    return version;
                
                return new Version(0, 0, 0);;
            }
        }
    }
}