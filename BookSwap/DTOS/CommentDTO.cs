using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class CommentDTO
    {
      
        public int ReaderID { get; set; }
      

        public int BookPostID { get; set; }


        public string Content { get; set; }
    }
}
