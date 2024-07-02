using System;
namespace BaseLibrary.DTOs
{
    public class TagDTO
    {
        public int TagID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int ListOfTagsID { get; set; }
        public ICollection<ContentTagDTO>? ContentTags { get; set; }
    }
}

