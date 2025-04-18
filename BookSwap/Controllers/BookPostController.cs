using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace BookSwap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookPostController : ControllerBase
    {
        private readonly BookSwapDbContext _db;

        public BookPostController(BookSwapDbContext db)
        {
            _db = db;
        }
      
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<BookPostResponseDto>>> GetAvailableBookPosts()
        {
            var currentDate = DateTime.UtcNow;

            var bookPosts = await _db.BookPosts
                .Where(bp => bp.PostStatus == "Available" &&
                            bp.StartDate <= currentDate &&
                            bp.EndDate >= currentDate)
                .Include(bp => bp.BookOwner) // Include BookOwner to access name
                .Select(bp => new BookPostResponseDto
                {
                    BookOwnerID=bp.BookOwnerID,
                    BookOwnerName = bp.BookOwner.BookOwnerName,
                    BookPostID = bp.BookPostID,
                    Title = bp.Title,
                    Genre = bp.Genre,
                    ISBN = bp.ISBN,
                    Description = bp.Description,
                    Language = bp.Language,
                    PublicationDate = bp.PublicationDate,
                    StartDate = bp.StartDate,
                    EndDate = bp.EndDate,
                    Price = bp.Price,
                    CoverPhoto = bp.CoverPhoto != null ? Convert.ToBase64String(bp.CoverPhoto) : null,
                    TotalLikes = bp.Likes.Count(l => l.IsLike),
                    TotalDislikes = bp.Likes.Count(l => !l.IsLike),
                })
                .ToListAsync();

            return Ok(bookPosts);
        }
        [HttpGet("comments/{postId}")]
        public async Task<IActionResult> GetCommentsOnBookPost
            (int postId)
        {
            var comments = await _db.Comments
                .Where(c => c.BookPostID == postId)
                .ToListAsync();

            var readerIds = comments.Select(c => c.ReaderID).ToList();

            var commentIds = comments.Select(c => c.CommentID).ToList();
            var replies = await _db.Replies
                .Where(r => commentIds.Contains(r.CommentID))
                .ToListAsync();

            readerIds.AddRange(replies.Select(r => r.ReaderID));
            readerIds = readerIds.Distinct().ToList();

            var readers = await _db.Readers
                .Where(r => readerIds.Contains(r.ReaderID))
                .ToDictionaryAsync(r => r.ReaderID, r => r.ReaderName);

            var repliesGrouped = replies
                .GroupBy(r => r.CommentID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => new ReplyResponseDTO
                    {
                        ReplyID = r.ReplyID,
                        ReaderID = r.ReaderID,
                        ReaderName = readers.ContainsKey(r.ReaderID) ? readers[r.ReaderID] : "Unknown",
                        Content = r.Content
                    }).ToList()
                );

            var result = comments.Select(c => new CommentResponseDTO
            {
                CommentID = c.CommentID,
                ReaderID = c.ReaderID,
                ReaderName = readers.ContainsKey(c.ReaderID) ? readers[c.ReaderID] : "Unknown",
                Content = c.Content,
                Replies = repliesGrouped.ContainsKey(c.CommentID) ? repliesGrouped[c.CommentID] : new List<ReplyResponseDTO>()
            }).ToList();

            return Ok(result);
        }


        [HttpGet("Search")]
        public async Task<IActionResult> SearchAvailableBookPosts(
       [FromQuery] string? genre,
       [FromQuery] string? title,
       [FromQuery] string? language,
       [FromQuery] int? price)
        {
            var currentDate = DateTime.UtcNow;
            var query = _db.BookPosts
                .Where(bp => bp.PostStatus == "Available" &&
                            bp.StartDate <= currentDate &&
                            bp.EndDate >= currentDate)
                .Include(bp => bp.BookOwner) // Include BookOwner to access name
                .AsQueryable();

            if (!string.IsNullOrEmpty(genre))
                query = query.Where(bp => bp.Genre.ToLower() == genre.ToLower());

            if (!string.IsNullOrEmpty(title))
                query = query.Where(bp => bp.Title.ToLower().Contains(title.ToLower()));

            if (!string.IsNullOrEmpty(language))
                query = query.Where(bp => bp.Language.ToLower() == language.ToLower());

            if (price.HasValue)
                query = query.Where(bp => bp.Price == price.Value);

            var filteredPosts = await query
                .Select(bp => new BookPostResponseDto
                {
                    BookOwnerID = bp.BookOwnerID,
                    BookOwnerName = bp.BookOwner.BookOwnerName,
                    BookPostID = bp.BookPostID,
                    Title = bp.Title,
                    Genre = bp.Genre,
                    ISBN = bp.ISBN,
                    Description = bp.Description,
                    Language = bp.Language,
                    PublicationDate = bp.PublicationDate,
                    StartDate = bp.StartDate,
                    EndDate = bp.EndDate,
                    Price = bp.Price,
                    CoverPhoto = bp.CoverPhoto != null ? Convert.ToBase64String(bp.CoverPhoto) : null,
                    TotalLikes = bp.Likes.Count(l => l.IsLike),
                    TotalDislikes = bp.Likes.Count(l => !l.IsLike),
                })
                .ToListAsync();

            return Ok(filteredPosts);
        }
    }
}
