using Autofac;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Statsify.Aggregator.Api.Services;
using Statsify.Aggregator.Api.Services.Impl;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Components.Impl;

namespace Statsify.Aggregator.Api
{
    public class Bootstrapper : AutofacNancyBootstrapper 
    {
        protected override void ConfigureRequestContainer(ILifetimeScope container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var builder = new ContainerBuilder();

            builder.Register(c => ConfigurationManager.Instance.Configuration).As<StatsifyAggregatorConfigurationSection>().SingleInstance();

            builder.RegisterType<MetricService>().As<IMetricService>().SingleInstance();
            builder.RegisterType<AnnotationService>().As<IAnnotationService>().SingleInstance();
            builder.Register(c => new MetricRegistry(c.Resolve<StatsifyAggregatorConfigurationSection>().Storage.Path)).AsImplementedInterfaces().SingleInstance();
            builder.Register(c => new AnnotationRegistry(c.Resolve<StatsifyAggregatorConfigurationSection>().Storage.Path)).AsImplementedInterfaces().SingleInstance();
            

            builder.Update(container.ComponentRegistry);            
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            pipelines.AfterRequest.
                AddItemToEndOfPipeline(ctx => {
                    ctx.Response.
                        WithHeader("Access-Control-Allow-Origin", "*").
                        WithHeader("Access-Control-Allow-Methods", "POST, GET").
                        WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-Type");
                });
        }
    }
}