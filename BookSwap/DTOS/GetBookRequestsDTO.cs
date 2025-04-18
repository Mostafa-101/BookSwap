namespace BookSwap.DTOS
{
    public class GetBookRequestsDTO
    {
        public int RequsetID { get; set; }
        public int BookPostID { get; set; }
        public string BookTitle { get; set; }
        public int ReaderID { get; set; }
        public string ReaderName { get; set; }
        public string RequsetStatus { get; set; }
    }
}
