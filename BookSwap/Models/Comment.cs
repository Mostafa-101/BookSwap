using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int CommentID { get; set; }
        [Required]

        [ForeignKey("Reader")]
        public int ReaderID { get; set; }
        [Required]

        [ForeignKey("BookPost")]

        public int BookPostID { get; set; }
        [Required]


        public string Content { get; set; }
        public Reader Reader { get; set; }
        public BookPost BookPost { get; set; }

    }
}
