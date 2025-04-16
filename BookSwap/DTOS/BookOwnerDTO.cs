using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class BookOwnerDTO
    {
        [Required]
        public string BookOwnerName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public int ssn { get; set; }

        [Required]
        public string RequestStatus { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
    }
}
