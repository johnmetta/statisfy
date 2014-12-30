using Nancy.Bootstrapper;
using Statsify.Core.Expressions;

namespace Statsify.Web.Api
{
    using System;
    using Nancy.Hosting.Self;

    class Program
    {
        static void Main(string[] args)
        {
            var uri =
                new Uri("http://localhost/Statsify/");

            NancyBootstrapperLocator.Bootstrapper = new Bootstrapper(); 

            Core.Expressions.Environment.RegisterFunctions(typeof(Functions));

            var configuration = new HostConfiguration();
            configuration.UrlReservations.CreateAutomatically = true;

            using (var host = new NancyHost(configuration, uri))
            {                
                host.Start();                

                Console.WriteLine("Your application is running on " + uri);
                Console.WriteLine("Press any [Enter] to close the host.");
                Console.ReadLine();
            }
        }
    }
}
