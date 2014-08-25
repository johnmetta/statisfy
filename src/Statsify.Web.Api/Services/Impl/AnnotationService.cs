using System;
using System.Collections.Generic;
using Statsify.Core.Storage;
using Statsify.Web.Api.Configuration;

namespace Statsify.Web.Api.Services
{
    internal class AnnotationService : IAnnotationService
    {
        private readonly string path;

        public AnnotationService(StatsifyConfigurationSection statsifyConfiguration)
        {
            path = statsifyConfiguration.Storage.Path;
        }

        public IEnumerable<Annotation> List(DateTime @from, DateTime until)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(path);
            return annotationDatabase.ReadAnnotations(from, until);
        }

        public void AddAnnotation(string title, string message)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(path);
            annotationDatabase.WriteAnnotation(DateTime.UtcNow, title, message);
        }
    }
}
