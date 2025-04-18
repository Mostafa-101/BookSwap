namespace BookSwap.DTOS
{
    public class BookRequestWithTitleResponseDTO
    {
        public int RequsetID { get; set; }
        public int BookPostID { get; set; }
        public int ReaderID { get; set; }
        public string RequsetStatus { get; set; }
        public string BookTitle { get; set; }
    }
}
