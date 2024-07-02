using System;
namespace BaseLibrary.DTOs
{
    public class ListOfTagsDTO
    {
        public int ListOfTagsID { get; set; }
        public int UserID { get; set; }
        public ICollection<TagDTO>? Tags { get; set; }
    }
}

