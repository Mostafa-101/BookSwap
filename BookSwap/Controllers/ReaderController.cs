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



        
        // Apply to borrow a book
        [Authorize]
        [HttpPost("borrow")]
        public async Task<IActionResult> BorrowBook([FromBody] BookRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify book post exists and is available
            var bookPost = await _context.BookPosts
                .FirstOrDefaultAsync(bp => bp.BookPostID == requestDto.BookPostID);

            if (bookPost == null)
            {
                return NotFound(new { message = "Book post not found" });
            }

            if (bookPost.PostStatus.ToLower() != "available")
            {
                return BadRequest(new { message = "Book is not available for borrowing" });
            }

            // Verify reader exists
            var reader = await _context.Readers
                .FirstOrDefaultAsync(r => r.ReaderID == requestDto.ReaderID);

            if (reader == null)
            {
                return NotFound(new { message = "Reader not found" });
            }

            // Create new book request
            var bookRequest = new BookRequest
            {
                BookPostID = requestDto.BookPostID,
                ReaderID = requestDto.ReaderID,
                RequsetStatus = "Pending", // Initial status
                BookPost = bookPost,
                Reader = reader
            };

            // Update book post status
        //    bookPost.PostStatus = "Borrowed";

            try
            {
                _context.BookRequests.Add(bookRequest);
                _context.BookPosts.Update(bookPost);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Borrow request created successfully",
                    requestId = bookRequest.RequsetID
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error processing borrow request", error = ex.Message });
            }
        }

        // Like a book
        [HttpPost("like")]
        [Authorize]
        public async Task<IActionResult> LikeOrDislikeBook([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };
            var bookPost = await _context.BookPosts.FindAsync(like.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

           
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(like.IsLike ? "Book liked successfully." : "Book disliked successfully.");
        }
        [HttpPut("like")]
        [Authorize]
        public async Task<IActionResult> ToggleReaction([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };
            var reaction = await _context.Likes
                .FirstOrDefaultAsync(l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID);

            if (reaction == null)
                return NotFound("Reaction not found.");

            reaction.IsLike = !reaction.IsLike; // Toggle like to dislike or vice versa
            await _context.SaveChangesAsync();

            return Ok(reaction.IsLike ? "Changed to like successfully." : "Changed to dislike successfully.");
        }
        [HttpDelete("like")]
        [Authorize]
        public async Task<IActionResult> DeleteReaction([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };
            var reaction = await _context.Likes
                .FirstOrDefaultAsync(l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID);

            if (reaction == null)
                return NotFound("Reaction not found.");

            _context.Likes.Remove(reaction);
            await _context.SaveChangesAsync();

            return Ok("Reaction deleted successfully.");
        }
        
        [HttpPost("comment")]
        [Authorize]
        public async Task<IActionResult> CommentOnBookPost( [FromBody] CommentDTO DTO)
        {
            var comment = new Comment
            {
                ReaderID=DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                Content = DTO.Content
            };
            var bookPost = await _context.BookPosts.FindAsync(comment.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok("Comment added successfully.");
        }

        [HttpPost("reply")]
        [Authorize]
        public async Task<IActionResult> ReolyOnComment([FromBody] ReplyDTO DTO)
        {
            var Reply = new Reply
            {
                ReaderID = DTO.ReaderID,
                CommentID = DTO.CommentID,
                Content = DTO.Content
            };
            var bookPost = await _context.Comments.FindAsync(Reply.CommentID);
            if (bookPost == null)
                return NotFound("Comment not found.");

            _context.Replies.Add(Reply);
            await _context.SaveChangesAsync();

            return Ok("Comment added successfully.");
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
