using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class ReplyResponseDTO
    {
        public int ReplyID { get; set; }
        public int CommentID { get; set; }
        public int ReaderID { get; set; }
        public string ReaderName { get; set; }
        public string Content { get; set; }
    }
}
