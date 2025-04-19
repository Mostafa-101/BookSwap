using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Admin
    {
        [Key]
        [Required]
        public string AdminName { get; set; }

        [Required]
        public string PasswordHash { get; set; }  // تغيير من "Password" إلى "PasswordHash"
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
