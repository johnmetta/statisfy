using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Options;
using Statsify.Client;

namespace Statsify
{
    public class Program
    {
        static void Main(string[] args)
        {
            var host = "localhost";
            var port = 8125;
            var title = "";
            var message = "";
            var tags = new List<string>();

            var options = new OptionSet {
                { "h|host=", v => host = v },
                { "p|port=", v => port = int.Parse(v) },
                { "title=", v => title = v},
                { "message=", v => message = v },
                { "tag=", tags.Add }
            };

            if(args.Length == 0)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            options.Parse(args);

            Console.WriteLine("connecting to {0}:{1}", host, port);

            var statsifyClient = new UdpStatsifyClient(host, port);
            statsifyClient.Annotation(title, message);
        }
    }
}
