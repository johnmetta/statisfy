using System;
using System.IO;
using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Statsify.Core.Storage;

namespace Statsify.Tests.Core.Storage
{
    [TestFixture]
    public class AnnotationDatabaseTest
    {
         [Test]
         public void CreateOpen()
         {             
             var path = new FileInfo(Path.GetTempFileName()).Directory.FullName;
             
             if(!AnnotationDataContext.Exists(path))
             {              
                 AnnotationDataContext.CreateDatabase(path);

                 Assert.IsTrue(AnnotationDataContext.Exists(path));
             }

             using(var dc=new AnnotationDataContext(path))
             {
                 dc.Annotations.Insert(() => new Annotation
                 {
                     Date = DateTime.UtcNow,
                     Message = "test"
                 });

                 Assert.IsTrue(dc.Annotations.ToArray().Any());
             }

             AnnotationDataContext.DropDatabase(path);
         }
    }
}
