namespace BookSwap.DTOS
{
    public class UpdateBookOwnerDTO
    {
        public string BookOwnerName { get; set; }
        public string Password { get; set; }
        public string ssn { get; set; }  // لو محتاج تتحدث فعلاً، سيبها، لو لا شيلها
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        // public string RequestStatus { get; set; }  // لو ناوي تخلي التحديث يشملها
    }
}
