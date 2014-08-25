using System;
using System.Collections.Generic;
using Statsify.Core.Storage;

namespace Statsify.Web.Api.Services
{
    public interface IAnnotationService
    {
        IEnumerable<Annotation> List(DateTime from, DateTime until);

        void AddAnnotation(string title, string message);
    }
}
