using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Reply
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int ReplyID { get; set; }
        [ForeignKey("Comment")]
        [Required]

        public int CommentID { get; set; }

        [ForeignKey("Reader")]
        [Required]

        public int ReaderID { get; set; }
        [Required]

        public string Content { get; set; }
        public Reader Reader { get; set; }
        public Comment Comment { get; set; }
    }
}
