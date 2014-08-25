using System;
using System.IO;
using NUnit.Framework;
using Statsify.Core.Storage;

namespace Statsify.Tests.Core.Storage
{
    [TestFixture]
    public class AnnotationDatabaseTests
    {
         [Test]
         public void CreateOpen()
         {
            var path = Path.GetTempFileName();

            var annotationDatabase = AnnotationDatabase.Create(path);
            var now = DateTime.UtcNow;

            for(var i = 0; i < 200; ++i)
            {
                annotationDatabase.WriteAnnotation(now.AddSeconds(i), "Deployment", "Deployed changeset cf43450d", "aeroclub-time", "deployment", "production");
                annotationDatabase.WriteAnnotation(now.AddSeconds(i * 2), "Deployment Started",
                    string.Format("Started deployment of Aeroclub Platform Backend - Production (Build {0}).\r\n\r\n" +
                                  "Changes:\r\n" +
                                  "http://dev.aeroclub.int/hglab/projects/time/repositories/aeroclub.time/compare/3b40122d48...c5162e7c83", i),
                                  "aeroclub-time", "deployment", "staging", "rollback");
            } // for

            var annotations = annotationDatabase.ReadAnnotations(now, now.AddMinutes(1));

             Assert.AreEqual(90, annotations.Count);
         }
    }
}
