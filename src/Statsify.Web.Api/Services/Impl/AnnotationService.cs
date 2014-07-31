namespace Statsify.Web.Api.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LinqToDB;
    using Core.Storage;
    using Configuration;

    internal class AnnotationService:IAnnotationService
    {
        private readonly string path;

        public AnnotationService(StatsifyConfigurationSection statsifyConfiguration)
        {
            path = statsifyConfiguration.Storage.Path;
        }

        public IEnumerable<Annotation> List(DateTime @from, DateTime until)
        {
            using(var dc = new AnnotationDataContext(path))
            {
                return dc.Annotations.Where(a => a.Date >= @from && a.Date <= until).ToArray();
            }
        }

        public void AddAnnotation(string message)
        {
            if(String.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(message);

            using (var dc = new AnnotationDataContext(path))
            {
                dc.Annotations.Insert(() => new Annotation
                {
                    Date = DateTime.UtcNow,
                    Message = message
                });                
            }
        }
    }
}
