using System;
namespace BaseLibrary.DTOs
{
    public class ListOfContentDTO
    {
        public int ListOfContentID { get; set; }
        public int UserID { get; set; }
        public ICollection<ContentDTO>? Contents { get; set; }
    }
}

