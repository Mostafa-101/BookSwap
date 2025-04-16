using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookOwnerDTO
    {
        public string BookOwnerName { get; set; }
        public string Password { get; set; }
        public int ssn { get; set; }
        public string RequestStatus { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
