using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class BookOwner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int BookOwnerID { get; set; }
        [Required]

        public string BookOwnerName { get; set; }
        [Required]

        public string Password { get; set; }
        [Required]

        public int ssn { get; set; }
        [Required]

        public string RequestStatus { get; set; }

        public List<BookPost> BookPosts { get; set; }
    }
}
