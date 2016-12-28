using System;

namespace Statsify.Client
{
    public class StatsifyChannelFactory
    {
        public IStatsifyChannel CreateChannel(Uri uri)
        {
            var scheme = uri.Scheme;
            return CreateChannel(uri, scheme);
        }

        private static IStatsifyChannel CreateChannel(Uri uri, string scheme)
        {
            if(scheme == "udp")
                return new UdpStatsifyChannel(uri);

            if(scheme == "http" || scheme == "https")
                return new HttpStatsifyChannel(uri);
            
            throw new ArgumentOutOfRangeException("scheme");
        }
    }
}