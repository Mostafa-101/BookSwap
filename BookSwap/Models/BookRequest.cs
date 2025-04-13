using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class BookRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int RequsetID { get; set; }


        [ForeignKey("BookPost")]
        [Required]

        public int BookPostID { get; set; }
        [ForeignKey("Reader")]

        [Required]

        public int ReaderID { get; set; }
        [Required]

        public string RequsetStatus { get; set; }
        public BookPost BookPost { get; set; }
        public Reader Reader { get; set; }


    }
}
