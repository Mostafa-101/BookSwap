using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookPostDTO
    {
        [Required]
        public int BookOwnerID { get; set; }

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

        public IFormFile? CoverPhoto { get; set; }  // optional
    }
}
