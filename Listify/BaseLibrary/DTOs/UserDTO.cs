using System;
namespace BaseLibrary.DTOs
{
    public class UserDTO
    {
        public int UserID { get; set; }
        public string? TelegramUserID { get; set; }
        public ListOfContentDTO? ListOfContent { get; set; }
        public ListOfTagsDTO? ListOfTags { get; set; }
    }
}

