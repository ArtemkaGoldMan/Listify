using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseLibrary.Entities
{
    public class ListOfTags
    {
        [Key]
        public int ListOfTagsID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        public virtual ICollection<Tag>? Tags { get; set; }
    }
}

