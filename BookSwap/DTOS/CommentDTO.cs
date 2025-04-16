using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class CommentDto
    {
        public int CommentID { get; set; }
        public int ReaderID { get; set; }
        public string ReaderName { get; set; }
        public string Content { get; set; }
        public List<ReplyDto> Replies { get; set; } // Added Replies
    }
}
