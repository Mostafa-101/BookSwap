using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BookSwap.Controllers
{
    [ApiController]
    [Route("api/reader")]
    public class ReaderController : ControllerBase
    {
        private readonly BookSwapDbContext _context;

        public ReaderController(BookSwapDbContext context)
        {
            _context = context;
        }

        // Reader sign up (registration)
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] ReaderDTO readerDTO)
        {
            var exists = await _context.Readers
                .AnyAsync(r => r.ReaderName == readerDTO.ReaderName);

            if (exists)
                return BadRequest("Reader already exists.");

            var reader = new Reader
            {
                ReaderName = readerDTO.ReaderName,
                Password = HashPassword(readerDTO.Password),
                Email = readerDTO.Email,
                PhoneNumber = readerDTO.PhoneNumber
            };

            _context.Readers.Add(reader);
            await _context.SaveChangesAsync();

            return Ok("Reader registered successfully.");
        }

        // Reader login (generate JWT token)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ReaderDTO readerDTO)
        {
            var existing = await _context.Readers
                .FirstOrDefaultAsync(r => r.ReaderName == readerDTO.ReaderName);

            if (existing == null || !VerifyPassword(readerDTO.Password, existing.Password))
                return Unauthorized("Invalid credentials.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("Your_Secret_Key");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("name", existing.ReaderName),
                    new System.Security.Claims.Claim("role", "Reader")
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = tokenString,
                User = new
                {
                    existing.ReaderID,
                    existing.ReaderName,
                    existing.Email
                }
            });
        }

        // Get all readers
        [HttpGet("all")]
        //[Authorize(Roles = "Admin")] // Only Admin can view all readers
        public async Task<IActionResult> GetAllReaders()
        {
            var readers = await _context.Readers.ToListAsync();

            if (readers.Count == 0)
                return NotFound("No readers found.");

            return Ok(readers);
        }

        // Get all available books
        [HttpGet("books")]
        public async Task<IActionResult> GetAvailableBooks()
        {
            var books = await _context.BookPosts
                .Where(b => b.IsAvailable)
                .ToListAsync();

            if (books.Count == 0)
                return NotFound("No available books found.");

            return Ok(books);
        }

        // Search books by genre and price
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string genre, [FromQuery] decimal? maxPrice)
        {
            var query = _context.BookPosts.AsQueryable();

            if (!string.IsNullOrEmpty(genre))
                query = query.Where(b => b.Genre.Contains(genre));

            if (maxPrice.HasValue)
                query = query.Where(b => b.Price <= maxPrice.Value);

            var books = await query.ToListAsync();

            return Ok(books);
        }

        // Apply to borrow a book
        [HttpPost("borrow/{bookPostId}")]
        [Authorize]
        public async Task<IActionResult> BorrowBook(int bookPostId, [FromBody] BookRequest bookRequest)
        {
            var bookPost = await _context.BookPosts.FindAsync(bookPostId);
            if (bookPost == null || !bookPost.IsAvailable)
                return BadRequest("This book is not available for borrowing.");

            _context.BookRequests.Add(bookRequest);
            await _context.SaveChangesAsync();

            return Ok("Request to borrow the book has been submitted.");
        }

        // Like a book
        [HttpPost("like/{bookPostId}")]
        [Authorize]
        public async Task<IActionResult> LikeBook(int bookPostId, [FromBody] Like like)
        {
            var bookPost = await _context.BookPosts.FindAsync(bookPostId);
            if (bookPost == null)
                return NotFound("Book not found.");

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return Ok("Book liked successfully.");
        }

        // Comment on a book
        [HttpPost("comment/{bookPostId}")]
        [Authorize]
        public async Task<IActionResult> CommentOnBook(int bookPostId, [FromBody] Comment comment)
        {
            var bookPost = await _context.BookPosts.FindAsync(bookPostId);
            if (bookPost == null)
                return NotFound("Book not found.");

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok("Comment added successfully.");
        }

        // Get all comments on a book
        [HttpGet("comments/{bookPostId}")]
        public async Task<IActionResult> GetComments(int bookPostId)
        {
            var comments = await _context.Comments
                .Where(c => c.BookPostID == bookPostId)
                .ToListAsync();

            return Ok(comments);
        }

        private string HashPassword(string password)
        {
            var key = Encoding.UTF8.GetBytes("Your_Secret_Key");
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }
    }
}
