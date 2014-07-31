using System;
using System.IO;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;

namespace Statsify.Core.Storage
{
    public class AnnotationDataContext : LinqToDB.Data.DataConnection
    {
        private const string DbName = "annotations.sqlite";

        public AnnotationDataContext(string path) : base(new SQLiteDataProvider(), String.Format("Data Source={0};Version=3;", Path.Combine(path, "statsify", DbName))) { }

        public ITable<Annotation> Annotations { get { return GetTable<Annotation>(); } }

        public static void CreateDatabase(string path)
        {
            var directoryPath = Path.Combine(path, "statsify");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, DbName);
       
            try
            {               
                System.Data.SQLite.SQLiteConnection.CreateFile(filePath);

                using (var dc = new AnnotationDataContext(path))
                {
                    dc.CreateTable<Annotation>();
                }       
            }
            catch
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                throw;
            }
                   
        }

        public static bool Exists(string path)
        {
            path = Path.Combine(path, "statsify", DbName);

            return File.Exists(path);
        }

        public static void DropDatabase(string path)
        {
            path = Path.Combine(path, "statsify", DbName);

            File.Delete(path);
        }
    }
}
