using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseLibrary.Entities
{
    public class Tag
    {
        [Key]
        public int TagID { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }

        public int ListOfTagsID { get; set; }
        [ForeignKey("ListOfTagsID")]
        public virtual ListOfTags? ListOfTags { get; set; }

        public virtual ICollection<ContentTag>? ContentTags { get; set; }
    }
}

