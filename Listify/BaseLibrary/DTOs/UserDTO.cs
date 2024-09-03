namespace BaseLibrary.DTOs
{
    public class UserDTO
    {
        public int UserID { get; set; }
        public string? TelegramUserID { get; set; }

        // Properties for managing limits
        public int MaxContents { get; set; } = 100;  // Default limit
        public int MaxTags { get; set; } = 20;      // Default limit

        public bool IsUnlimited { get; set; } = false;  // Flag for unlimited access
    }
}
