using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class LikeDTO
    {

        public int ReaderID { get; set; }
        public int BookPostID { get; set; }
        public bool IsLike { get; set; }
    }
}
