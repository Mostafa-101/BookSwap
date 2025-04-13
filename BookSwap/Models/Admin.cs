using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Admin
    {
        [Key]
        [Required]
        public string AdminName { get; set; }
        [Required]

        public string Password { get; set; }
    }
}
