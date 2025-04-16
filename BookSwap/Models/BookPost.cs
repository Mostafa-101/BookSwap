using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class BookPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookPostID { get; set; }

        public int BookOwnerID { get; set; }

        [ForeignKey("BookOwnerID")]
        public BookOwner BookOwner { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Genre { get; set; }

        [Required]
        public string ISBN { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Language { get; set; }

        [Required]
        public DateTime PublicationDate { get; set; }


        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int Price { get; set; }

        public byte[]? CoverPhoto { get; set; }

        [Required]
        public string PostStatus { get; set; }

        public List<BookRequest> BookRequests { get; set; } = new();
        public List<Like> Likes { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }
}
