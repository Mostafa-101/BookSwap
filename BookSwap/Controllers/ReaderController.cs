using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using BookSwap.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookSwap.Controllers
{
    [ApiController]
    [Route("api/reader")]
    public class ReaderController : ControllerBase
    {
        private readonly BookSwapDbContext _context;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _Audience;
        private readonly GenericRepo<Reader> _readerRepo;
        private readonly GenericRepo<BookPost> _bookPostRepo;
        private readonly GenericRepo<BookRequest> _bookRequestRepo;
        private readonly GenericRepo<Like> _likeRepo;
        private readonly GenericRepo<Comment> _commentRepo;
        private readonly GenericRepo<Reply> _replyRepo;
        private readonly GenericRepo<RefreshToken> _refreshTokenRepo;

        public ReaderController(
            BookSwapDbContext context,
            IConfiguration configuration,
            GenericRepo<Reader> readerRepo,
            GenericRepo<BookPost> bookPostRepo,
            GenericRepo<BookRequest> bookRequestRepo,
            GenericRepo<Like> likeRepo,
            GenericRepo<Comment> commentRepo,
            GenericRepo<Reply> replyRepo,
            GenericRepo<RefreshToken> refreshTokenRepo)
        {
            _context = context;
            _secretKey = configuration["Jwt:Key"];
            _issuer = configuration["jwt:Issuer"];
            _Audience = configuration["jwt:Audience"];
            _readerRepo = readerRepo;
            _bookPostRepo = bookPostRepo;
            _bookRequestRepo = bookRequestRepo;
            _likeRepo = likeRepo;
            _commentRepo = commentRepo;
            _replyRepo = replyRepo;
            _refreshTokenRepo = refreshTokenRepo;
        }

        private IActionResult VerifyReaderId(int readerId)
        {
            var jwtReaderId = User.FindFirst("readerId")?.Value;
            if (jwtReaderId == null || !int.TryParse(jwtReaderId, out int parsedJwtReaderId) || parsedJwtReaderId != readerId)
            {
                return Unauthorized(new { message = "You are not authorized to access this resource" });
            }
            return null; 
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] ReaderSignUpDTO readerDTO)
        {
            var exists = (await _readerRepo.getAllFilterAsync(r => r.ReaderName == readerDTO.ReaderName)).Any();
            if (exists)
                return BadRequest("Reader already exists.");

            var reader = new Reader
            {
                ReaderName = readerDTO.ReaderName,
                Password = PasswordService.HashPassword(readerDTO.Password),
                Email = readerDTO.Email,
                PhoneNumber = readerDTO.PhoneNumber
            };

            bool added = _readerRepo.add(reader);
            if (!added)
                return StatusCode(500, new { message = "Error registering reader" });

            return Ok("Reader registered successfully.");
        }

        [AllowAnonymous]
        [HttpPost("reader/login")]
        public async Task<IActionResult> ReaderLogin([FromBody] ReaderLoginDTO readerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = (await _readerRepo.getAllFilterAsync(r => r.ReaderName == readerDTO.ReaderName)).FirstOrDefault();
            if (existing == null || !PasswordService.VerifyPassword(readerDTO.Password, existing.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            var tokenString = PasswordService.GenerateJwtToken(
                _secretKey,
                _issuer,
                _Audience,
                existing.ReaderName,
                "Reader",
                existing.ReaderID.ToString()
            );

            var refreshToken = PasswordService.GenerateRefreshToken();
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = existing.ReaderID.ToString(),
                UserType = "Reader",
                ReaderId = existing.ReaderID
            };

            bool tokenAdded = _refreshTokenRepo.add(refreshTokenEntity);
            if (!tokenAdded)
                return StatusCode(500, new { message = "Error storing refresh token" });

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenEntity.Expires
            });

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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [Authorize(Roles = "Reader")]
        [HttpPut("UpdateReader/{id}")]
        public IActionResult UpdateReader(int id, [FromBody] UpdateReaderDTO updatedReader)
        {
            var verificationResult = VerifyReaderId(id);
            if (verificationResult != null)
                return verificationResult;

            var existingReader = _readerRepo.getById(id);
            if (existingReader == null)
                return NotFound("Reader not found.");

            existingReader.ReaderName = updatedReader.ReaderName ?? existingReader.ReaderName;
            if (!string.IsNullOrEmpty(updatedReader.Password))
                existingReader.Password = PasswordService.HashPassword(updatedReader.Password);
            existingReader.Email = updatedReader.Email ?? existingReader.Email;
            existingReader.PhoneNumber = updatedReader.PhoneNumber ?? existingReader.PhoneNumber;

            bool isUpdated = _readerRepo.update(existingReader);
            if (!isUpdated)
                return StatusCode(500, "Failed to update Reader.");

            return Ok("Reader updated successfully.");
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("borrow")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> BorrowBook([FromBody] BookRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var verificationResult = VerifyReaderId(requestDto.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var bookPost = _bookPostRepo.getById(requestDto.BookPostID);
            if (bookPost == null)
                return NotFound(new { message = "Book post not found" });

            if (bookPost.PostStatus.ToLower() != "available")
                return BadRequest(new { message = "Book is not available for borrowing" });

            var reader = _readerRepo.getById(requestDto.ReaderID);
            if (reader == null)
                return NotFound(new { message = "Reader not found" });

            var bookRequest = new BookRequest
            {
                BookPostID = requestDto.BookPostID,
                ReaderID = requestDto.ReaderID,
                RequsetStatus = "Pending",
                BookPost = bookPost,
                Reader = reader
            };

            try
            {
                bool requestAdded = _bookRequestRepo.add(bookRequest);
                bool postUpdated = _bookPostRepo.update(bookPost);
                if (requestAdded && postUpdated)
                    return Ok(new
                    {
                        message = "Borrow request created successfully",
                        requestId = bookRequest.RequsetID
                    });
                else
                    return StatusCode(500, new { message = "Error processing borrow request" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing borrow request", error = ex.Message });
            }
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpPost("return")]
        [Authorize(Roles = "Reader")]
        public async Task<ActionResult<BookRequestResponseDTO>> ReturnBook([FromBody] BookRequestResponseDTO requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var verificationResult = VerifyReaderId(requestDto.ReaderID);
            if (verificationResult != null)
                return Unauthorized(new { message = "You are not authorized to access this resource" });

            var bookRequest = (await _bookRequestRepo.getAllFilterAsync(
                filter: br => br.RequsetID == requestDto.RequsetID,
                include: q => q.Include(br => br.BookPost)
            )).FirstOrDefault();

            if (bookRequest == null)
                return NotFound(new { message = "Book request not found" });

            if (bookRequest.BookPostID != requestDto.BookPostID || bookRequest.ReaderID != requestDto.ReaderID)
                return BadRequest(new { message = "Invalid book request details" });

            if (bookRequest.RequsetStatus != "Accepted")
                return BadRequest(new { message = "Book is not currently borrowed" });

            bookRequest.RequsetStatus = "Returned";
            if (bookRequest.BookPost != null)
                bookRequest.BookPost.PostStatus = "Available";

            try
            {
                bool requestUpdated = _bookRequestRepo.update(bookRequest);
                bool postUpdated = _bookPostRepo.update(bookRequest.BookPost);
                if (!requestUpdated || !postUpdated)
                    return StatusCode(500, new { message = "Error updating the database" });

                var response = new BookRequestResponseDTO
                {
                    RequsetID = bookRequest.RequsetID,
                    BookPostID = bookRequest.BookPostID,
                    ReaderID = bookRequest.ReaderID,
                    RequsetStatus = bookRequest.RequsetStatus
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating the database", error = ex.Message });
            }
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpGet("user/{readerId}")]
        [Authorize(Roles = "Reader")]
        public async Task<ActionResult<IEnumerable<BookRequestWithTitleResponseDTO>>> GetBookRequestsForUser(int readerId)
        {
            var verificationResult = VerifyReaderId(readerId);
            if (verificationResult != null)
              return Unauthorized(new { message = "You are not authorized to access this resource" });

            var readerExists = (await _readerRepo.getAllFilterAsync(r => r.ReaderID == readerId)).Any();
            if (!readerExists)
                return NotFound(new { message = "Reader not found" });

            var bookRequests = await _bookRequestRepo.getAllFilterAsync(
                filter: br => br.ReaderID == readerId,
                include: q => q.Include(br => br.BookPost)
            );

            var bookRequestDtos = bookRequests.Select(br => new BookRequestWithTitleResponseDTO
            {
                RequsetID = br.RequsetID,
                BookPostID = br.BookPostID,
                ReaderID = br.ReaderID,
                RequsetStatus = br.RequsetStatus,
                BookTitle = br.BookPost.Title
            }).ToList();

            if (!bookRequestDtos.Any())
                return Ok(new List<BookRequestWithTitleResponseDTO>());

            return Ok(bookRequestDtos);
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpPost("like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> LikeOrDislikeBook([FromBody] LikeDTO DTO)
        {
            var verificationResult = VerifyReaderId(DTO.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };

            var bookPost = _bookPostRepo.getById(like.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

            bool added = _likeRepo.add(like);
            if (!added)
                return StatusCode(500, new { message = "Error adding like" });

            return Ok(like.IsLike ? "Book liked successfully." : "Book disliked successfully.");
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpGet("check-like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> CheckLikeStatus([FromQuery] int readerId, [FromQuery] int bookPostId)
        {
            var verificationResult = VerifyReaderId(readerId);
            if (verificationResult != null)
                return verificationResult;

            var bookPost = _bookPostRepo.getById(bookPostId);
            if (bookPost == null)
                return NotFound("Book not found.");

            var like = (await _likeRepo.getAllFilterAsync(l => l.ReaderID == readerId && l.BookPostID == bookPostId))
                .FirstOrDefault();

            if (like == null)
                return Ok(new { message = "No like or dislike record found for this book post." });

            return Ok(new
            {
                ReaderID = like.ReaderID,
                BookPostID = like.BookPostID,
                IsLike = like.IsLike,
            });
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> ToggleReaction([FromBody] LikeDTO DTO)
        {
            var verificationResult = VerifyReaderId(DTO.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var reaction = (await _likeRepo.getAllFilterAsync(
                l => l.BookPostID == DTO.BookPostID && l.ReaderID == DTO.ReaderID
            )).FirstOrDefault();

            if (reaction == null)
                return NotFound("Reaction not found.");

            reaction.IsLike = !reaction.IsLike;

            bool updated = _likeRepo.update(reaction);
            if (!updated)
                return StatusCode(500, new { message = "Error updating reaction" });

            return Ok(reaction.IsLike ? "Changed to like successfully." : "Changed to dislike successfully.");
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpDelete("like")]
        [Authorize(Roles = "Reader")]

        public async Task<IActionResult> DeleteReaction([FromBody] LikeDTO DTO)
        {
            var verificationResult = VerifyReaderId(DTO.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var reaction = (await _likeRepo.getAllFilterAsync(
                l => l.BookPostID == DTO.BookPostID && l.ReaderID == DTO.ReaderID
            )).FirstOrDefault();

            if (reaction == null)
                return NotFound("Reaction not found.");

            bool deleted = _likeRepo.remove(reaction.LikeID);
            if (!deleted)
                return StatusCode(500, new { message = "Error deleting reaction" });

            return Ok("Reaction deleted successfully.");
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpPost("comment")]
        [Authorize(Roles = "Reader")]

        public async Task<IActionResult> CommentOnBookPost([FromBody] CommentDTO DTO)
        {
            var verificationResult = VerifyReaderId(DTO.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var comment = new Comment
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                Content = DTO.Content
            };

            var bookPost = _bookPostRepo.getById(comment.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

            bool added = _commentRepo.add(comment);
            if (!added)
                return StatusCode(500, new { message = "Error adding comment" });

            return Ok("Comment added successfully.");
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpPost("reply")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> ReplyOnComment([FromBody] ReplyDTO DTO)
        {
            var verificationResult = VerifyReaderId(DTO.ReaderID);
            if (verificationResult != null)
                return verificationResult;

            var reply = new Reply
            {
                ReaderID = DTO.ReaderID,
                CommentID = DTO.CommentID,
                Content = DTO.Content
            };

            var comment = _commentRepo.getById(reply.CommentID);
            if (comment == null)
                return NotFound("Comment not found.");

            bool added = _replyRepo.add(reply);
            if (!added)
                return StatusCode(500, new { message = "Error adding reply" });

            return Ok("Reply added successfully.");
        }
    }
}