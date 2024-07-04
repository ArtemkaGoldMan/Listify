using System;
namespace BaseLibrary.DTOs
{
    public class ContentDTO
    {
        public int ContentID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int ListOfContentID { get; set; }
    }
}


