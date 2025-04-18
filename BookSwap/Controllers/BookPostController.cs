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
        [HttpPost]
        public async Task<IActionResult> CreateBookPost([FromForm] BookPostDTO dto)
        {
            using var stream = new MemoryStream();
            await dto.CoverPhoto.CopyToAsync(stream);
            var post = new BookPost
            {
                BookOwnerID = dto.BookOwnerID,
                Title = dto.Title,
                Genre = dto.Genre,
                ISBN = dto.ISBN,
                Description = dto.Description,
                Language = dto.Language,
                PublicationDate = dto.PublicationDate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Price = dto.Price,
                PostStatus="Pending",
                CoverPhoto =stream.ToArray()
            };
            await _db.BookPosts.AddAsync(post);
            await _db.SaveChangesAsync();
            return Ok("Book post created successfully!");
        }
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<BookPostResponseDto>>> GetAvailableBookPosts()
        {
            var currentDate = DateTime.UtcNow;

            var bookPosts = await _db.BookPosts
                .Where(bp => bp.PostStatus == "Available" &&
                            bp.StartDate <= currentDate &&
                            bp.EndDate >= currentDate)
                .Select(bp => new BookPostResponseDto
                {
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


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookPost(int id)
        {
            var post = await _db.BookPosts.FindAsync(id);

            if (post == null)
            {
                return NotFound($"No BookPost found with ID = {id}");
            }

            if (post.PostStatus == "Borrowed") 
            {
                return BadRequest("Cannot delete a borrowed book post.");
            }

            _db.BookPosts.Remove(post);
            await _db.SaveChangesAsync();

            return Ok($"BookPost with ID = {id} deleted successfully.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookPost(int id, [FromForm] BookPostDTO dto)
        {
            var post = await _db.BookPosts.FindAsync(id);
            using var stream = new MemoryStream();
            await dto.CoverPhoto.CopyToAsync(stream);
            if (post == null)
                return NotFound($"BookPost with ID = {id} not found.");

            post.Title = dto.Title;
            post.Genre = dto.Genre;
            post.ISBN = dto.ISBN;
            post.Description = dto.Description;
            post.Language = dto.Language;
            post.PublicationDate = dto.PublicationDate;
            post.StartDate = dto.StartDate;
            post.EndDate = dto.EndDate;
            post.Price = dto.Price;
            post.CoverPhoto = stream.ToArray();



            await _db.SaveChangesAsync();

            return Ok($"BookPost with ID = {id} updated successfully.");
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
