using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace BaseLibrary.Entities
{
    public class ListOfContent
    {
        [Key]
        public int ListOfContentID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        public virtual ICollection<Content>? Contents { get; set; }
    }
}

