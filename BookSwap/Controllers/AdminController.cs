using BookSwap.Controllers;
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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/admin")]

public class AdminController : ControllerBase
{
    private readonly BookSwapDbContext _context;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _Audience;
    private readonly GenericRepo<Admin> _adminRepo;
    private readonly GenericRepo<RefreshToken> _refreshTokenRepo;
    private readonly GenericRepo<BookOwner> _bookOwnerRepo;
    private readonly GenericRepo<BookPost> _bookPostRepo;

    public AdminController(
        BookSwapDbContext context,
        IConfiguration configuration,
        GenericRepo<Admin> adminRepo,
        GenericRepo<RefreshToken> refreshTokenRepo,
        GenericRepo<BookOwner> bookOwnerRepo,
        GenericRepo<BookPost> bookPostRepo)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
        _issuer = configuration["jwt:Issuer"];
        _Audience = configuration["jwt:Audience"];
        _adminRepo = adminRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _bookOwnerRepo = bookOwnerRepo;
        _bookPostRepo = bookPostRepo;
    }

    // -------------------- Auth Operations --------------------

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateAdmin([FromBody] AdminDTO DTO)
    {
        var existingAdmin = (await _adminRepo.getAllFilterAsync(
            a => a.AdminName == DTO.AdminName
        )).FirstOrDefault();

        if (existingAdmin != null)
            return BadRequest("Admin already exists.");

        var admin = new Admin
        {
            AdminName = DTO.AdminName,
            PasswordHash = PasswordService.HashPassword(DTO.PasswordHash)
        };

        bool added = _adminRepo.add(admin);
        if (!added)
        {
            return StatusCode(500, new { message = "Error registering admin" });
        }

        return Ok("Admin registered successfully.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminDTO DTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = (await _adminRepo.getAllFilterAsync(
            a => a.AdminName == DTO.AdminName
        )).FirstOrDefault();

        if (existing == null || !PasswordService.VerifyPassword(DTO.PasswordHash, existing.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("name", existing.AdminName),
                new Claim("role", "Admin")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _issuer,
            Audience = _Audience
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        var refreshToken = PasswordService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            UserId = existing.AdminName,
            UserType = "Admin",
            AdminName = existing.AdminName
        };

        bool tokenAdded = _refreshTokenRepo.add(refreshTokenEntity);
        if (!tokenAdded)
        {
            return StatusCode(500, new { message = "Error storing refresh token" });
        }

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
                AdminName = existing.AdminName
            }
        });
    }

    // -------------------- BookOwner Management --------------------

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpGet("ManageBookOwners")]
    public async Task<ActionResult<IEnumerable<BookOwnerDTOResponse>>> GetPendingBookOwners()
    {
        var pendingBookOwners = await _bookOwnerRepo.getAllFilterAsync(
            b => b.RequestStatus == "Pending"
        );

        var response = pendingBookOwners.Select(b => new BookOwnerDTOResponse
        {
            BookOwnerID = b.BookOwnerID,
            BookOwnerName = b.BookOwnerName,
            ssn = b.GetDecryptedSsn(),
            RequestStatus = b.RequestStatus,
            Email = b.GetDecryptedEmail(),
            PhoneNumber = b.GetDecryptedSsn()
        }).ToList();

        return Ok(response);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpPut("ProcessBookOwner/{id}")]
    public async Task<IActionResult> ProcessBookOwner(int id, [FromQuery] string action)
    {
        var bookOwner =  _bookOwnerRepo.getById(id);
        if (bookOwner == null)
            return NotFound("Book Owner not found.");

        if (bookOwner.RequestStatus != "Pending")
            return BadRequest("Book Owner request is already processed.");

        if (action.ToLower() == "approve")
        {
            bookOwner.RequestStatus = "Approved";
        }
        else if (action.ToLower() == "reject")
        {
            bookOwner.RequestStatus = "Rejected";
        }
        else
        {
            return BadRequest("Invalid action. Use 'approve' or 'reject'.");
        }

        bool updated = _bookOwnerRepo.update(bookOwner);
        if (!updated)
        {
            return StatusCode(500, new { message = "Error updating book owner" });
        }

        return Ok($"Book Owner {action}d.");
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpGet("ManageBookPosts")]
    public async Task<ActionResult<IEnumerable<BookPostResponseDto>>> GetPendingBookPosts()
    {
        var pendingBookPosts = await _bookPostRepo.getAllFilterAsync(
            filter: b => b.PostStatus == "Pending",
            include: q => q.Include(b => b.BookOwner)
        );

        var response = pendingBookPosts.Select(b => new BookPostResponseDto
        {
            BookOwnerID = b.BookOwnerID,
            BookOwnerName = b.BookOwner.BookOwnerName,
            BookPostID = b.BookPostID,
            Title = b.Title,
            Genre = b.Genre,
            ISBN = b.ISBN,
            Description = b.Description,
            Language = b.Language,
            PublicationDate = b.PublicationDate,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            Price = b.Price,
            CoverPhoto = b.CoverPhoto != null ? Convert.ToBase64String(b.CoverPhoto) : null,
            TotalLikes = b.Likes.Count(l => l.IsLike),
            TotalDislikes = b.Likes.Count(l => !l.IsLike),
        }).ToList();

        return Ok(response);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpPut("ProcessBookPosts/{id}")]
    public async Task<IActionResult> ProcessBookPosts(int id, [FromQuery] string action)
    {
        var bookPost =  _bookPostRepo.getById(id);
        if (bookPost == null)
            return NotFound("Book Post not found.");

        if (bookPost.PostStatus != "Pending")
            return BadRequest("Book Post request is already processed.");

        if (action.ToLower() == "approve")
        {
            bookPost.PostStatus = "Available";
        }
        else if (action.ToLower() == "reject")
        {
            bookPost.PostStatus = "Rejected";
        }
        else
        {
            return BadRequest("Invalid action. Use 'approve' or 'reject'.");
        }

        bool updated = _bookPostRepo.update(bookPost);
        if (!updated)
        {
            return StatusCode(500, new { message = "Error updating book post" });
        }

        return Ok($"Book Post {action}d.");
    }
    // -------------------- Helpers --------------------

    private string HashPassword(string password)
    {
        var key = Encoding.UTF8.GetBytes(_secretKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    /*  private bool VerifyPassword(string inputPassword, string storedPasswordHash)
      {
          var inputHash = HashPassword(inputPassword);
          return inputHash == storedPasswordHash;
      }*/
    /*  // -------------------- Admin CRUD --------------------

        [HttpGet("{adminName}")]
        public async Task<IActionResult> GetAdmin(string adminName)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == adminName);
            if (admin == null) return NotFound("Admin not found.");
            return Ok(admin);
        }

        [HttpPut("{adminName}")]
        public async Task<IActionResult> UpdateAdmin(string adminName, [FromBody] Admin updatedAdmin)
        {
            var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == adminName);
            if (existingAdmin == null) return NotFound("Admin not found.");

            existingAdmin.AdminName = updatedAdmin.AdminName;
            if (!string.IsNullOrEmpty(updatedAdmin.PasswordHash))
            {
                existingAdmin.PasswordHash = HashPassword(updatedAdmin.PasswordHash);
            }

            _context.Admins.Update(existingAdmin);
            await _context.SaveChangesAsync();

            return Ok("Admin updated successfully.");
        }

        [HttpDelete("{adminName}")]
        public async Task<IActionResult> DeleteAdmin(string adminName)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == adminName);
            if (admin == null) return NotFound("Admin not found.");

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            return Ok("Admin deleted successfully.");
        }
        */
}
