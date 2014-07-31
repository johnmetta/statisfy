using System;
using LinqToDB.Mapping;

namespace Statsify.Core.Storage
{
    [Table("Annotation")]
    public class Annotation
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column, NotNull]
        public DateTime Date { get; set; }

        [Column(DbType = "Text"), NotNull]
        public string Message { get; set; }
    }
}
