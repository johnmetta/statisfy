using Nancy.Bootstrapper;

namespace Statsify.Web.Api
{
    using Autofac;
    using Nancy;
    using Nancy.Bootstrappers.Autofac;
    using Configuration;
    using Services;

    public class Bootstrapper : AutofacNancyBootstrapper 
    {
        protected override void ConfigureRequestContainer(ILifetimeScope container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var builder = new ContainerBuilder();

            builder.Register(c => new MetricService(c.Resolve<StatsifyConfigurationSection>())).As<IMetricService>();
            builder.Register(c => new AnnotationService(c.Resolve<StatsifyConfigurationSection>())).As<IAnnotationService>();
            builder.Register(c => new SeriesService(c.Resolve<IMetricService>())).As<ISeriesService>();
            builder.Register(c => new ConfigurationManager().Configuration).As<StatsifyConfigurationSection>().SingleInstance();

            builder.Update(container.ComponentRegistry);            
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
        }
    }
}