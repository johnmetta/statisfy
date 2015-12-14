using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statsify.Core.Storage;

namespace Statsify.Core.Components.Impl
{
    public class AnnotationRegistry : IAnnotationRegistry
    {
        private readonly string rootDirectory;

        public AnnotationRegistry(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public void WriteAnnotation(Annotation annotation)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(Path.Combine(rootDirectory, "annotations.db"));
            annotationDatabase.WriteAnnotation(annotation.Timestamp, annotation.Title, annotation.Message, annotation.Tags.ToArray());
        }

        public IEnumerable<Annotation> ReadAnnotations(DateTime @from, DateTime until)
        {
            var annotationDatabase = AnnotationDatabase.OpenOrCreate(Path.Combine(rootDirectory, "annotations.db"));
            return annotationDatabase.ReadAnnotations(from, until);
        }
    }
}