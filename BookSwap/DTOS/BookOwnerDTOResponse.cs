namespace BookSwap.DTOS
{
    public class BookOwnerDTOResponse
    {
        public int BookOwnerID { get; set; }

        public string BookOwnerName { get; set; }
        public int ssn { get; set; }
        public string RequestStatus { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

    }
}
