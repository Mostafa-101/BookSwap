using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
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

        public ReaderController(BookSwapDbContext context, IConfiguration configuration)
        {
            _context = context;
            _secretKey = configuration["Jwt:Key"];
            _issuer = configuration["jwt:Issuer"];
            _Audience = configuration["jwt:Audience"];
        }

        // Reader sign up (registration)
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] ReaderSignUpDTO readerDTO)
        {
            var exists = await _context.Readers
                .AnyAsync(r => r.ReaderName == readerDTO.ReaderName);

            if (exists)
                return BadRequest("Reader already exists.");

            var reader = new Reader
            {
                ReaderName = readerDTO.ReaderName,
                Password = PasswordService.HashPassword(readerDTO.Password),
                Email = readerDTO.Email,
                PhoneNumber = readerDTO.PhoneNumber
            };

            _context.Readers.Add(reader);
            await _context.SaveChangesAsync();

            return Ok("Reader registered successfully.");
        }

        // Reader login (generate JWT token)
        [AllowAnonymous]
        [HttpPost("reader/login")]
        public async Task<IActionResult> ReaderLogin([FromBody] ReaderLoginDTO readerDTO)
        {
            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find reader by ReaderName
            var existing = await _context.Readers
                .FirstOrDefaultAsync(r => r.ReaderName == readerDTO.ReaderName);

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
                UserId = existing.ReaderID.ToString(), // Using ReaderID as UserId
                UserType = "Reader",
                ReaderId = existing.ReaderID
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

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
        [HttpPost("return")]
        [Authorize(Roles = "Reader")]
        public async Task<ActionResult<BookRequestResponseDTO>> ReturnBook([FromBody] BookRequestResponseDTO requestDto)
        {
            // Validate input DTO
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find the book request
            var bookRequest = await _context.BookRequests
                .Include(br => br.BookPost)
                .FirstOrDefaultAsync(br => br.RequsetID == requestDto.RequsetID);

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
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error updating the database", error = ex.Message });
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
        [HttpGet("user/{readerId}")]
        [Authorize(Roles = "Reader")]
        public async Task<ActionResult<IEnumerable<BookRequestWithTitleResponseDTO>>> GetBookRequestsForUser(int readerId)
        {
            // Check if reader exists
            var readerExists = await _context.Readers.AnyAsync(r => r.ReaderID == readerId);
            if (!readerExists)
            {
                return NotFound(new { message = "Reader not found" });
            }

            // Retrieve book requests with book titles
            var bookRequests = await _context.BookRequests
                .Where(br => br.ReaderID == readerId)
                .Include(br => br.BookPost)
                .Select(br => new BookRequestWithTitleResponseDTO
                {
                    RequsetID = br.RequsetID,
                    BookPostID = br.BookPostID,
                    ReaderID = br.ReaderID,
                    RequsetStatus = br.RequsetStatus,
                    BookTitle =  br.BookPost.Title 
                })
                .ToListAsync();

            if (!bookRequests.Any())
            {
                return Ok(new List<BookRequestWithTitleResponseDTO>());
            }

            return Ok(bookRequests);
        }
        // Like a book
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
            var bookPost = await _context.BookPosts.FindAsync(like.BookPostID);
            if (bookPost == null)
                return NotFound("Book not found.");

           
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(like.IsLike ? "Book liked successfully." : "Book disliked successfully.");
        }
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
            var reaction = await _context.Likes
                .FirstOrDefaultAsync(l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID);

            if (reaction == null)
                return NotFound("Reaction not found.");

            reaction.IsLike = !reaction.IsLike; // Toggle like to dislike or vice versa
            await _context.SaveChangesAsync();

            return Ok(reaction.IsLike ? "Changed to like successfully." : "Changed to dislike successfully.");
        }
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
            var reaction = await _context.Likes
                .FirstOrDefaultAsync(l => l.BookPostID == like.BookPostID && l.ReaderID == like.ReaderID);

            if (reaction == null)
                return NotFound("Reaction not found.");

            _context.Likes.Remove(reaction);
            await _context.SaveChangesAsync();

            return Ok("Reaction deleted successfully.");
        }
        
        [HttpPost("comment")]
        [Authorize(Roles = "Reader")]
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
        [Authorize(Roles = "Reader")]
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
