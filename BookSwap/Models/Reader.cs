using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Reader
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int ReaderID { get; set; }
        [Required]


        public string ReaderName { get; set; }
        [Required]

        public string Password { get; set; }
        public List<Like> Likes { get; set; }
        public List<Comment> Comments { get; set; }
        public List<BookRequest> BookRequests { get; set; }
    }
}
