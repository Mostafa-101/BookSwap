namespace BookSwap.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; }
        public string UserId { get; set; } // Generic user ID (can be Admin, BookOwner, or Reader)
        public string UserType { get; set; } // "Admin", "BookOwner", or "Reader"

        // Explicit foreign key properties
        public string? AdminId { get; set; } // Foreign key for Admin
        public int? BookOwnerId { get; set; } // Foreign key for BookOwner
        public int? ReaderId { get; set; } // Foreign key for Reader

        // Navigation properties
        public Admin Admin { get; set; }
        public BookOwner BookOwner { get; set; }
        public Reader Reader { get; set; }
    }
}