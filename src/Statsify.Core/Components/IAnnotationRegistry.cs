using System;
using System.Collections.Generic;
using Statsify.Core.Storage;

namespace Statsify.Core.Components
{
    public interface IAnnotationRegistry
    {
        void WriteAnnotation(Annotation annotation);

        IEnumerable<Annotation> ReadAnnotations(DateTime from, DateTime until);
    }
}