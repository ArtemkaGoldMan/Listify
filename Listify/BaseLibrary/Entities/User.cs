using System;
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.Entities
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        public string? TelegramUserID { get; set; }

        // Properties for managing limits
        public int MaxContents { get; set; } = 50;  // Default limit
        public int MaxTags { get; set; } = 20;      // Default limit

        public bool IsUnlimited { get; set; } = false;  // Flag for unlimited access

        public virtual ListOfContent? ListOfContent { get; set; }
        public virtual ListOfTags? ListOfTags { get; set; }
    }
}

