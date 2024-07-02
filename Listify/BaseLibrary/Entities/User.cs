using System;
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.Entities
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        public string? TelegramUserID { get; set; }

        public virtual ListOfContent? ListOfContent { get; set; }
        public virtual ListOfTags? ListOfTags { get; set; }
    }
}

