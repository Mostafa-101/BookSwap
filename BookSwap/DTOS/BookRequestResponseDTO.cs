using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookRequestResponseDTO
    {
        public int RequsetID { get; set; }

        public int BookPostID { get; set; }    
        public int ReaderID { get; set; }

        public string RequsetStatus { get; set; }
    }
}
