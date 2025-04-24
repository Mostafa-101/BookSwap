using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using BookSwap.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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

        // Reader sign up (registration)
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] ReaderSignUpDTO readerDTO)
        {
            var exists = (await _readerRepo.getAllFilterAsync(
                r => r.ReaderName == readerDTO.ReaderName
            )).Any();

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
            {
                return StatusCode(500, new { message = "Error registering reader" });
            }

            return Ok("Reader registered successfully.");
        }

        // Reader login (generate JWT token)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> ReaderLogin([FromBody] ReaderLoginDTO readerDTO)
        {
            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find reader by ReaderName
            var existing = (await _readerRepo.getAllFilterAsync(
                r => r.ReaderName == readerDTO.ReaderName
            )).FirstOrDefault();

            if (existing == null || !PasswordService.VerifyPassword(readerDTO.Password, existing.Password))
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            // Generate JWT access token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("name", existing.ReaderName),
                    new Claim("role", "Reader"),
                    new Claim("readerId", existing.ReaderID.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _issuer,
                Audience = _Audience
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Generate and store refresh token
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
            {
                return StatusCode(500, new { message = "Error storing refresh token" });
            }

            // Set refresh token in HTTP-only cookie
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenEntity.Expires
            });

            // Return token and reader details
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
        //// Get all readers
        //[Authorize(Roles = "Reader")]
        //[HttpGet("all")]
        ////[Authorize(Roles = "Admin")] // Only Admin can view all readers
        //public async Task<IActionResult> GetAllReaders()
        //{
        //    var readers = await _context.Readers.ToListAsync();

        //    if (readers.Count == 0)
        //        return NotFound("No readers found.");

        //    return Ok(readers);
        //}


        // Apply to borrow a book
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("borrow")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> BorrowBook([FromBody] BookRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify book post exists and is available
            var bookPost =  _bookPostRepo.getById(requestDto.BookPostID);

            if (bookPost == null)
            {
                return NotFound(new { message = "Book post not found" });
            }

            if (bookPost.PostStatus.ToLower() != "available")
            {
                return BadRequest(new { message = "Book is not available for borrowing" });
            }

            // Verify reader exists
            var reader =  _readerRepo.getById(requestDto.ReaderID);

            if (reader == null)
            {
                return NotFound(new { message = "Reader not found" });
            }

            // Create new book request
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
                {
                    return Ok(new
                    {
                        message = "Borrow request created successfully",
                        requestId = bookRequest.RequsetID
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Error processing borrow request" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing borrow request", error = ex.Message });
            }
        }
        /*       [AllowAnonymous]
               [HttpPost("refresh-token")]
               public async Task<IActionResult> RefreshToken()
               {
                   var refreshToken = Request.Cookies["refreshToken"];
                   if (string.IsNullOrEmpty(refreshToken))
                   {
                       return BadRequest(new { message = "Refresh token is missing." });
                   }

                   var refreshTokenEntity = await _context.RefreshTokens
                       .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                   if (refreshTokenEntity == null)
                   {
                       return Unauthorized(new { message = "Invalid refresh token." });
                   }

                   if (refreshTokenEntity.IsExpired)
                   {
                       return Unauthorized(new { message = "Refresh token has expired." });
                   }

                   var tokenHandler = new JwtSecurityTokenHandler();
                   var key = Encoding.ASCII.GetBytes(_secretKey);
                   SecurityTokenDescriptor tokenDescriptor = null;

                   switch (refreshTokenEntity.UserType)
                   {
                       case "Admin":
                           var admin = await _context.Admins
                               .FirstOrDefaultAsync(a => a.AdminName == refreshTokenEntity.AdminName);
                           if (admin == null)
                           {
                               return Unauthorized(new { message = "Admin not found." });
                           }
                           tokenDescriptor = new SecurityTokenDescriptor
                           {
                               Subject = new ClaimsIdentity(new[]
                               {
                           new Claim("name", admin.AdminName),
                           new Claim("role", "Admin")
                       }),
                               Expires = DateTime.UtcNow.AddHours(1),
                               SigningCredentials = new SigningCredentials(
                                   new SymmetricSecurityKey(key),
                                   SecurityAlgorithms.HmacSha256Signature),
                               Issuer = _issuer,
                               Audience = _Audience
                           };
                           break;

                       case "BookOwner":
                           var bookOwner = await _context.BookOwners
                               .FirstOrDefaultAsync(bo => bo.BookOwnerID == refreshTokenEntity.BookOwnerId);
                           if (bookOwner == null)
                           {
                               return Unauthorized(new { message = "BookOwner not found." });
                           }
                           if (bookOwner.RequestStatus != "Approved")
                           {
                               return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account is not approved." });
                           }
                           tokenDescriptor = new SecurityTokenDescriptor
                           {
                               Subject = new ClaimsIdentity(new[]
                               {
                           new Claim("name", bookOwner.BookOwnerName),
                           new Claim("role", "BookOwner"),
                           new Claim("bookOwnerId", bookOwner.BookOwnerID.ToString())
                       }),
                               Expires = DateTime.UtcNow.AddHours(2),
                               SigningCredentials = new SigningCredentials(
                                   new SymmetricSecurityKey(key),
                                   SecurityAlgorithms.HmacSha256Signature),
                               Issuer = _issuer,
                               Audience = _Audience
                           };
                           break;

                       case "Reader":
                           var reader = await _context.Readers
                               .FirstOrDefaultAsync(r => r.ReaderID == refreshTokenEntity.ReaderId);
                           if (reader == null)
                           {
                               return Unauthorized(new { message = "Reader not found." });
                           }
                           tokenDescriptor = new SecurityTokenDescriptor
                           {
                               Subject = new ClaimsIdentity(new[]
                               {
                           new Claim("name", reader.ReaderName),
                           new Claim("role", "Reader"),
                           new Claim("readerId", reader.ReaderID.ToString())
                       }),
                               Expires = DateTime.UtcNow.AddHours(2),
                               SigningCredentials = new SigningCredentials(
                                   new SymmetricSecurityKey(key),
                                   SecurityAlgorithms.HmacSha256Signature),
                               Issuer = _issuer,
                               Audience = _Audience
                           };
                           break;

                       default:
                           return BadRequest(new { message = "Invalid user type." });
                   }

                   var token = tokenHandler.CreateToken(tokenDescriptor);
                   var tokenString = tokenHandler.WriteToken(token);

                   var newRefreshToken = PasswordService.GenerateRefreshToken();
                   var newRefreshTokenEntity = new RefreshToken
                   {
                       Token = newRefreshToken,
                       Expires = DateTime.UtcNow.AddDays(7),
                       Created = DateTime.UtcNow,
                       UserId = refreshTokenEntity.UserId,
                       UserType = refreshTokenEntity.UserType,
                       AdminName = refreshTokenEntity.AdminName,
                       BookOwnerId = refreshTokenEntity.BookOwnerId,
                       ReaderId = refreshTokenEntity.ReaderId
                   };

                   _context.RefreshTokens.Remove(refreshTokenEntity);
                   _context.RefreshTokens.Add(newRefreshTokenEntity);
                   await _context.SaveChangesAsync();

                   Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
                   {
                       HttpOnly = true,
                       Secure = true,
                       SameSite = SameSiteMode.Strict,
                       Expires = newRefreshTokenEntity.Expires
                   });

                   return Ok(new { Token = tokenString });
               }*/
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("return")]
        [Authorize(Roles = "Reader")]
        public async Task<ActionResult<BookRequestResponseDTO>> ReturnBook([FromBody] BookRequestResponseDTO requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find the book request
            var bookRequest = (await _bookRequestRepo.getAllFilterAsync(
                filter: br => br.RequsetID == requestDto.RequsetID,
                include: q => q.Include(br => br.BookPost)
            )).FirstOrDefault();

            if (bookRequest == null)
            {
                return NotFound(new { message = "Book request not found" });
            }

            // Verify DTO data matches the database
            if (bookRequest.BookPostID != requestDto.BookPostID || bookRequest.ReaderID != requestDto.ReaderID)
            {
                return BadRequest(new { message = "Invalid book request details" });
            }

            // Check if the book is currently borrowed
            if (bookRequest.RequsetStatus != "Accepted")
            {
                return BadRequest(new { message = "Book is not currently borrowed" });
            }

            // Update book request status
            bookRequest.RequsetStatus = "Returned";

            // Update book post status to available
            if (bookRequest.BookPost != null)
            {
                bookRequest.BookPost.PostStatus = "Available";
            }

            try
            {
                bool requestUpdated = _bookRequestRepo.update(bookRequest);
                bool postUpdated = _bookPostRepo.update(bookRequest.BookPost);

                if (!requestUpdated || !postUpdated)
                {
                    return StatusCode(500, new { message = "Error updating the database" });
                }

                // Create response DTO
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
            // Check if reader exists
            var readerExists = (await _readerRepo.getAllFilterAsync(
                r => r.ReaderID == readerId
            )).Any();

            if (!readerExists)
            {
                return NotFound(new { message = "Reader not found" });
            }

            // Retrieve book requests with book titles
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
            {
                return Ok(new List<BookRequestWithTitleResponseDTO>());
            }

            return Ok(bookRequestDtos);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> LikeOrDislikeBook([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };

            var bookPost =  _bookPostRepo.getById(like.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

            bool added = _likeRepo.add(like);
            if (!added)
            {
                return StatusCode(500, new { message = "Error adding like" });
            }

            return Ok(like.IsLike ? "Book liked successfully." : "Book disliked successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> ToggleReaction([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };

            var reaction = (await _likeRepo.getAllFilterAsync(
                l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID
            )).FirstOrDefault();

            if (reaction == null)
                return NotFound("Reaction not found.");

            reaction.IsLike = !reaction.IsLike;

            bool updated = _likeRepo.update(reaction);
            if (!updated)
            {
                return StatusCode(500, new { message = "Error updating reaction" });
            }

            return Ok(reaction.IsLike ? "Changed to like successfully." : "Changed to dislike successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("like")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> DeleteReaction([FromBody] LikeDTO DTO)
        {
            var like = new Like
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                IsLike = DTO.IsLike
            };

            var reaction = (await _likeRepo.getAllFilterAsync(
                l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID
            )).FirstOrDefault();

            if (reaction == null)
                return NotFound("Reaction not found.");

            bool deleted = _likeRepo.remove(reaction.LikeID); // Assuming Like has an Id property
            if (!deleted)
            {
                return StatusCode(500, new { message = "Error deleting reaction" });
            }

            return Ok("Reaction deleted successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("comment")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> CommentOnBookPost([FromBody] CommentDTO DTO)
        {
            var comment = new Comment
            {
                ReaderID = DTO.ReaderID,
                BookPostID = DTO.BookPostID,
                Content = DTO.Content
            };

            var bookPost =  _bookPostRepo.getById(comment.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

            bool added = _commentRepo.add(comment);
            if (!added)
            {
                return StatusCode(500, new { message = "Error adding comment" });
            }

            return Ok("Comment added successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("reply")]
        [Authorize(Roles = "Reader")]
        public async Task<IActionResult> ReplyOnComment([FromBody] ReplyDTO DTO)
        {
            var reply = new Reply
            {
                ReaderID = DTO.ReaderID,
                CommentID = DTO.CommentID,
                Content = DTO.Content
            };

            var comment =  _commentRepo.getById(reply.CommentID);
            if (comment == null)
                return NotFound("Comment not found.");

            bool added = _replyRepo.add(reply);
            if (!added)
            {
                return StatusCode(500, new { message = "Error adding reply" });
            }

            return Ok("Reply added successfully.");
        }

        /*  private string HashPassword(string password)
          {
              var key = Encoding.UTF8.GetBytes("Your_Secret_Key");
              using var hmac = new HMACSHA256(key);
              var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
              return Convert.ToBase64String(hash);
          }

          private bool VerifyPassword(string inputPassword, string storedHash)
          {
              return HashPassword(inputPassword) == storedHash;
          }*/
    }
}
