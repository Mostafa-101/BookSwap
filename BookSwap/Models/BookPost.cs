using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class BookPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]

        public int BookPostID { get; set; }


        [ForeignKey("BookOwner")]

        public int BookOwnerID { get; set; }
        [Required]


        public string Title { get; set; }
        [Required]

        public string Genre { get; set; }
        [Required]

        public string ISBN { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        public string Language { get; set; }
        [Required]

        public DateTime PublicationDate { get; set; }
        [Required]

        public bool IsAvailable { get; set; }
        [Required]


        public DateTime StartDate { get; set; }
        [Required]

        public DateTime EndDate { get; set; }
        [Required]

        public int Price { get; set; }
        [Required]

        public byte[] CoverPhoto { get; set; }
        [Required]

        public string PostStatus { get; set; }


        public BookOwner BookOwner { get; set; }
        public List<BookRequest> BookRequests { get; set; }
        public List<Like> Likes { get; set; }
        public List<Comment> Comments { get; set; }



    }
}
