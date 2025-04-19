using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookOwnerSignUpDTO
    {
        public string BookOwnerName { get; set; }
        public string Password { get; set; }
        public string ssn { get; set; }
        public string RequestStatus { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
