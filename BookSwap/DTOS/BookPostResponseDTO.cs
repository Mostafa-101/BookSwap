namespace BookSwap.DTOS
{
    // DTO for returning book post data
    public class BookPostResponseDto
    {
        public int BookPostID { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string ISBN { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public DateTime PublicationDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Price { get; set; }
        public string CoverPhoto { get; set; } // Base64-encoded string for the image
        public int TotalLikes { get; set; }
        public int TotalDislikes { get; set; }
    }
}
