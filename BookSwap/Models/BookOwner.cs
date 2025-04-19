using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using BookSwap.Controllers;

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
        public string EncryptedSsn { get; set; }

        [Required]
        public string RequestStatus { get; set; }

        [Required]
        public string EncryptedEmail { get; set; }

        [Required]
        public string EncryptedPhoneNumber { get; set; }

        // Helper methods to decrypt when needed
        public string GetDecryptedSsn() => PasswordService.Decrypt(EncryptedSsn);
        public string GetDecryptedEmail() => PasswordService.Decrypt(EncryptedEmail);
        public string GetDecryptedPhoneNumber() => PasswordService.Decrypt(EncryptedPhoneNumber);


        public List<BookPost> BookPosts { get; set; } = new();
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
