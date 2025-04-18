using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookRequestDTO
    {
        
        public int BookPostID { get; set; }
        
        public int ReaderID { get; set; }

    }
}
