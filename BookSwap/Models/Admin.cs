using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Models
{
    public class Admin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AdminID { get; set; }
        [Required]

        public string AdminName { get; set; }

        [Required]
        public string PasswordHash { get; set; }  // تغيير من "Password" إلى "PasswordHash"

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    }
}
