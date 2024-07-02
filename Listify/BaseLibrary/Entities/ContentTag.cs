using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseLibrary.Entities
{
    public class ContentTag
    {
        [Key]
        [Column(Order = 1)]
        public int ContentID { get; set; }
        [ForeignKey("ContentID")]
        public virtual Content? Content { get; set; }

        [Key]
        [Column(Order = 2)]
        public int TagID { get; set; }
        [ForeignKey("TagID")]
        public virtual Tag? Tag { get; set; }
    }
}

