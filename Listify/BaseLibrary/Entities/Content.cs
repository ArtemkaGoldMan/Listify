﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseLibrary.Entities
{
    public class Content
    {
        [Key]
        public int ContentID { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        public int ListOfContentID { get; set; }
        [ForeignKey("ListOfContentID")]
        public virtual ListOfContent? ListOfContent { get; set; }

        public virtual ICollection<ContentTag>? ContentTags { get; set; }
    }
}

