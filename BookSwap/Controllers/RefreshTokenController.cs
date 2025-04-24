using BookSwap.Data.Contexts;
using BookSwap.Models;
using BookSwap.Repos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq.Expressions;

namespace BookSwap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefreshTokenController : ControllerBase
    {
        private readonly BookSwapDbContext _context;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _Audience;
        private readonly GenericRepo<RefreshToken> _refreshTokenRepo;
        private readonly GenericRepo<Admin> _adminRepo;
        private readonly GenericRepo<BookOwner> _bookOwnerRepo;
        private readonly GenericRepo<Reader> _readerRepo;

        public RefreshTokenController(
            BookSwapDbContext context,
            IConfiguration configuration,
            GenericRepo<RefreshToken> refreshTokenRepo,
            GenericRepo<Admin> adminRepo,
            GenericRepo<BookOwner> bookOwnerRepo,
            GenericRepo<Reader> readerRepo)
        {
            _context = context;
            _secretKey = configuration["Jwt:Key"];
            _issuer = configuration["jwt:Issuer"];
            _Audience = configuration["jwt:Audience"];
            _refreshTokenRepo = refreshTokenRepo;
            _adminRepo = adminRepo;
            _bookOwnerRepo = bookOwnerRepo;
            _readerRepo = readerRepo;
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Refresh token is missing." });
            }

            var refreshTokenEntity = (await _refreshTokenRepo.getAllFilterAsync(
                rt => rt.Token == refreshToken
            )).FirstOrDefault();

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
                    var admin = (await _adminRepo.getAllFilterAsync(
                        a => a.AdminName == refreshTokenEntity.AdminName
                    )).FirstOrDefault();
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
                    if (!refreshTokenEntity.BookOwnerId.HasValue)
                    {
                        return Unauthorized(new { message = "BookOwner ID is missing." });
                    }
                    var bookOwner =  _bookOwnerRepo.getById(refreshTokenEntity.BookOwnerId.Value);
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
                    if (!refreshTokenEntity.ReaderId.HasValue)
                    {
                        return Unauthorized(new { message = "Reader ID is missing." });
                    }
                    var reader =  _readerRepo.getById(refreshTokenEntity.ReaderId.Value);
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

            bool removed = _refreshTokenRepo.remove(refreshTokenEntity.Id);
            bool added = _refreshTokenRepo.add(newRefreshTokenEntity);

            if (!removed || !added)
            {
                return StatusCode(500, new { message = "Error updating refresh token" });
            }

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshTokenEntity.Expires
            });

            return Ok(new { Token = tokenString });
        }
    }
}