using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookPostDTO
    {
        public int BookOwnerID { get; set; }

        public string Title { get; set; }

        public string Genre { get; set; }

        public string ISBN { get; set; }
        public string Description { get; set; }

        public string Language { get; set; }

        public DateTime PublicationDate { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int Price { get; set; }

        public IFormFile CoverPhoto { get; set; }
    }
}
