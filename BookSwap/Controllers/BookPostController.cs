using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using BookSwap.Repos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace BookSwap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookPostController : ControllerBase
    {
        private readonly BookSwapDbContext _db;
        private readonly GenericRepo<BookPost> _bookPostRepo;
        private readonly GenericRepo<Comment> _commentRepo;
        private readonly GenericRepo<Reply> _replyRepo;
        private readonly GenericRepo<Reader> _readerRepo;

        public BookPostController(
            BookSwapDbContext db,
            GenericRepo<BookPost> bookPostRepo,
            GenericRepo<Comment> commentRepo,
            GenericRepo<Reply> replyRepo,
            GenericRepo<Reader> readerRepo)
        {
            _db = db;
            _bookPostRepo = bookPostRepo;
            _commentRepo = commentRepo;
            _replyRepo = replyRepo;
            _readerRepo = readerRepo;
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<BookPostResponseDto>>> GetAvailableBookPosts()
        {
            var currentDate = DateTime.UtcNow;

            var bookPosts = await _bookPostRepo.getAllFilterAsync(
                filter: bp => bp.PostStatus == "Available" &&
                             bp.StartDate <= currentDate &&
                             bp.EndDate >= currentDate,
                include: q => q.Include(bp => bp.BookOwner)
                              .Include(bp => bp.Likes)
            );

            var response = bookPosts.Select(bp => new BookPostResponseDto
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
                TotalDislikes = bp.Likes.Count(l => !l.IsLike)
            }).ToList();

            return Ok(response);
        }

        [HttpGet("comments/{postId}")]
        public async Task<IActionResult> GetCommentsOnBookPost(int postId)
        {
            var comments = await _commentRepo.getAllFilterAsync(
                c => c.BookPostID == postId
            );

            var readerIds = comments.Select(c => c.ReaderID).ToList();
            var commentIds = comments.Select(c => c.CommentID).ToList();

            var replies = await _replyRepo.getAllFilterAsync(
                r => commentIds.Contains(r.CommentID)
            );

            readerIds.AddRange(replies.Select(r => r.ReaderID));
            readerIds = readerIds.Distinct().ToList();

            var readers = await _readerRepo.getAllFilterAsync(
                r => readerIds.Contains(r.ReaderID)
            );

            var readersDict = readers.ToDictionary(r => r.ReaderID, r => r.ReaderName);

            var repliesGrouped = replies
                .GroupBy(r => r.CommentID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => new ReplyResponseDTO
                    {
                        ReplyID = r.ReplyID,
                        ReaderID = r.ReaderID,
                        ReaderName = readersDict.ContainsKey(r.ReaderID) ? readersDict[r.ReaderID] : "Unknown",
                        Content = r.Content
                    }).ToList()
                );

            var result = comments.Select(c => new CommentResponseDTO
            {
                CommentID = c.CommentID,
                ReaderID = c.ReaderID,
                ReaderName = readersDict.ContainsKey(c.ReaderID) ? readersDict[c.ReaderID] : "Unknown",
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

            Expression<Func<BookPost, bool>> filter = bp =>
                bp.PostStatus == "Available" &&
                bp.StartDate <= currentDate &&
                bp.EndDate >= currentDate &&
                (string.IsNullOrEmpty(genre) || bp.Genre.ToLower() == genre.ToLower()) &&
                (string.IsNullOrEmpty(title) || bp.Title.ToLower().Contains(title.ToLower())) &&
                (string.IsNullOrEmpty(language) || bp.Language.ToLower() == language.ToLower()) &&
                (!price.HasValue || bp.Price <= price.Value);

            var filteredPosts = await _bookPostRepo.getAllFilterAsync(
                filter: filter,
                include: q => q.Include(bp => bp.BookOwner)
                              .Include(bp => bp.Likes)
            );

            var response = filteredPosts.Select(bp => new BookPostResponseDto
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
                TotalDislikes = bp.Likes.Count(l => !l.IsLike)
            }).ToList();

            return Ok(response);
        }

    }
}

/*[HttpGet("Search")]
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
            //var result = _bookPostRepo.getAllFilter(filter: e => e.Genre.ToLower() == genre.ToLower() || e.Title.ToLower().Contains(title.ToLower()) || e.Language.ToLower() == language.ToLower() || e.Price == price.Value,include: e=> e.Include(i=>i.Likes));
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
        }*/