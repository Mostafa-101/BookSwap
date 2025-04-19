using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class BookOwner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookOwnerID { get; set; }

        [Required]
        public string BookOwnerName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public int ssn { get; set; }

        [Required]
        public string RequestStatus { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public List<BookPost> BookPosts { get; set; } = new();
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
