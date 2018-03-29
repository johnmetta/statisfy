using System;
using System.IO;
using NUnit.Framework;
using Statsify.Core.Storage;

namespace Statsify.Tests.Core.Storage
{
    [TestFixture]
    [SingleThreaded]
    public class AnnotationDatabaseTests
    {
         [Test]
         public void CreateOpen()
         {
            var path = Path.GetTempFileName();

            var annotationDatabase = AnnotationDatabase.Create(path);
            var now = DateTime.UtcNow.AddHours(-2);

            for(var i = 0; i <= 10; ++i)
            {
                annotationDatabase.WriteAnnotation(now.AddSeconds(i), "Deployment", "Deployed changeset cf43450d", "platform-backend", "deployment", "production");
                annotationDatabase.WriteAnnotation(now.AddSeconds(i * 2), "Deployment Started", "Started deployment of Platform Backend", "deployment", "staging", "rollback");
            } // for

            var annotations = annotationDatabase.ReadAnnotations(now, now.AddMinutes(1));

             Assert.AreEqual(20, annotations.Count);
         }
    }
}
