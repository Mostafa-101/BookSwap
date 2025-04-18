using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Like
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int LikeID { get; set; }
        [ForeignKey("Reader")]
        [Required]

        public int ReaderID { get; set; }
        [Required]

        [ForeignKey("BookPost")]

        public int BookPostID { get; set; }
        [Required]

        public bool IsLike { get; set; }

        public Reader? Reader { get; set; }
        public BookPost? BookPost { get; set; }
    }
}
