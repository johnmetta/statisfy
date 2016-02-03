using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Mono.Options;
using Statsify.Client;

namespace Statsify
{
    public class Program
    {
        static int Main(string[] args)
        {
            var dispatcher = new CommandDispatcher((o, os) => {
                os.Add("verbose", "Enable additional output", v => o.Verbose = v != null);
            });

            dispatcher.Register<MetricCommand, MetricCommand.MetricCommandOptions>(cd => new MetricCommand(), 
                "metric",
                "Publish Metric to the Statsify Aggregator",
                o => new OptionSet {
                    { 
                        "t=|type=", 
                        "Metric type (one of 'counter', 'timer', 'gauge', 'set')",
                        v => {
                            MetricCommand.MetricType type;
                            if(Enum.TryParse(v, true, out type))
                                o.Type = type;
                            else
                                throw new Exception("Unknown metric type " + v);
                        }
                    }
                    
                });

            dispatcher.Register<HelpCommand, Options>(cd => new HelpCommand(cd), "help", "Show help");
            

            return dispatcher.Execute(args);

            /*var host = "localhost";
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
            statsifyClient.Annotation(title, message);*/
        }
    }

    public class HelpCommand : Command<Options>
    {
        private readonly ICommandDispatcher commandDispatcher;

        [Parameter(Name = "Command", Position = 0, Description = "The name of the command to show help for", Optional = true)]
        public string Command { get; set; }

        public HelpCommand(ICommandDispatcher commandDispatcher)
        {
            this.commandDispatcher = commandDispatcher;
        }

        public override int Execute(Options options, Arguments args)
        {
            Console.WriteLine("Statsify");
            Console.WriteLine();

            if(string.IsNullOrWhiteSpace(Command))
            {
                foreach(var commandDescriptor in commandDispatcher.Commands.OrderBy(cd => cd.Name))
                {
                    Console.WriteLine("  {0,-10} {1}", commandDescriptor.Name, commandDescriptor.Description);
                }
            } // if
            else
            {
                var commandDescriptor = commandDispatcher.Commands.SingleOrDefault(cd => string.Equals(cd.Name, Command, StringComparison.InvariantCultureIgnoreCase));
                if(commandDescriptor == null) return -1;

                Console.WriteLine("Summary:");
                Console.WriteLine("      " + commandDescriptor.Description);
                Console.WriteLine();

                Console.WriteLine("Syntax:");
                Console.WriteLine("      {0} {1} [OPTION]... {2}", 
                    commandDispatcher.Executable,
                    Command,
                    string.Join(" ", commandDescriptor.Parameters.Select(a => string.Format(a.Optional ? "[{0}]" : "{0}", a.Name.ToUpperInvariant()))));
                Console.WriteLine();

                Console.WriteLine("Parameters:");
                foreach(var parameter in commandDescriptor.Parameters)
                    Console.WriteLine("      {0,-23}{1}", parameter.Name.ToUpperInvariant(), parameter.Description);

                Console.WriteLine();
                Console.WriteLine("Options:");
                commandDescriptor.OptionSet.WriteOptionDescriptions(Console.Out);
            } // else

            return 0;
        }
    }


    public class MetricCommand : Command<MetricCommand.MetricCommandOptions>
    {
        public class MetricCommandOptions : Options
        {
            public MetricType Type { get; set; }

            public MetricCommandOptions()
            {
                Type = MetricType.Counter;
            }
        }

        public enum MetricType { Counter, Timer, Gauge, Set }

        [Parameter(Name = "name", Position = 0, Description = "Name of the Metric")]
        public string Name { get; set; }

        [Parameter(Name = "value", Position = 1, Description = "Value of the Metric")]
        public double Value { get; set; }

        [Parameter(Name = "server", Position = 2, Description = "Endpoint of the Statsify Aggregator", Optional = true)]
        public Endpoint Server { get; set; }

        public MetricCommand()
        {
            Server = new Endpoint(Network.GetDomainQualifiedName("statsify"), UdpStatsifyClient.DefaultPort);
        }

        public override int Execute(MetricCommandOptions options, Arguments args)
        {
            var server = Server;

            if(server.Port == 0)
                server = new Endpoint(server.Address, UdpStatsifyClient.DefaultPort);

            using(var statsifyClient = new UdpStatsifyClient(server.Address, server.Port))
            {
                switch(options.Type)
                {
                    case MetricType.Counter:
                        statsifyClient.Counter(Name, Value);
                        break;
                    case MetricType.Timer:
                        statsifyClient.Time(Name, Value);
                        break;
                    case MetricType.Gauge:
                        statsifyClient.Gauge(Name, Value);
                        break;
                    case MetricType.Set:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                } // switch
            } // using

            return 0;
        }
    }

    [TypeConverter(typeof(EndpointTypeConverter))]
    public class Endpoint
    {
        public string Address { get; private set; }

        public int Port { get; private set; }

        public Endpoint(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Address, Port);
        }

        public static Endpoint Parse(string endpoint, int defaultPort)
        {
            var address = endpoint.SubstringBefore(":");
            var portNumber = endpoint.Contains(":") ? endpoint.SubstringAfter(":") : "";

            var port = defaultPort;
            if(!string.IsNullOrWhiteSpace(portNumber))
                if(!int.TryParse(portNumber, out port))
                    port = defaultPort;

            return new Endpoint(address, port);
        }
    }

    public class EndpointTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if(sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(value is string)
                return Endpoint.Parse((string)value, 0);

            return base.ConvertFrom(context, culture, value);
        }
    }

    internal static class StringExtensions
    {
        public static string SubstringBefore(this string source, string v)
        {
            var substringOffset = source.IndexOf(v, System.StringComparison.Ordinal);

            return substringOffset == -1 ?
                source:
                source.Substring(0, substringOffset);
        }

        public static string SubstringAfter(this string source, string v)
        {
            var start = source.IndexOf(v, System.StringComparison.Ordinal);

            return start == -1 ?
                source :
                source.Substring(start + v.Length);
        }
    }

    public static class Network
    {
        public const int DefaultAgentPort = 28082;

        public const int DefaultServerPort = 28081;

        public static string Fqdn 
        {
            get
            {
                var fqdn = HostName;

                if(!HostName.Contains(DomainName))
                    fqdn = HostName + "." + DomainName;

                return fqdn.ToLowerInvariant();
            }
        }

        public static string DomainName
        {
            get
            {
                return IPGlobalProperties.GetIPGlobalProperties().DomainName.ToLowerInvariant();
            }
        }

        public static string HostName
        {
            get
            {
                return Dns.GetHostName().ToLowerInvariant();
            }
        }

        public static string MachineName
        {
            get { return GetDomainQualifiedName(HostName); }
        }

        public static string GetDomainQualifiedName(string hostName)
        {
            var domainQualifiedName =
                string.IsNullOrWhiteSpace(DomainName) ? 
                    hostName.ToLowerInvariant() : 
                    string.Format("{0}.{1}", hostName.TrimEnd('.'), DomainName.TrimStart('.')).ToLowerInvariant();

            return domainQualifiedName;
        }
    }



}
