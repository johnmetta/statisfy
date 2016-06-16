using System;

namespace Statsify.Aggregator
{
    public static class Application
    {
        public static Version Version
        {
            get { return typeof(Application).Assembly.GetName().Version;  }
        }
    }
}