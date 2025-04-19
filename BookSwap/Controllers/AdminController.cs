using BookSwap.Controllers;
using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
    public AdminController(BookSwapDbContext context, IConfiguration configuration)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
        _issuer = configuration["jwt:Issuer"];
        _Audience = configuration["jwt:Audience"];
    }

    // -------------------- Auth Operations --------------------

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] AdminDTO DTO)
    {
        var admin = new Admin { AdminName = DTO.AdminName, PasswordHash = DTO.PasswordHash };
        var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);
        if (existingAdmin != null) return BadRequest("Admin already exists.");

        admin.PasswordHash = PasswordService.HashPassword(admin.PasswordHash);
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return Ok("Admin registered successfully.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminDTO DTO)
    {
        var admin = new Admin { AdminName = DTO.AdminName, PasswordHash = DTO.PasswordHash };
        var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);
        if (existingAdmin == null || !PasswordService.VerifyPassword(admin.PasswordHash, existingAdmin.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        // Generate JWT access token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
           new System.Security.Claims.Claim("name", existingAdmin.AdminName),
           new System.Security.Claims.Claim("role", "Admin")
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

        // Generate and store refresh token
        var refreshToken = PasswordService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            UserId = existingAdmin.AdminName, // Using AdminName as UserId
            UserType = "Admin",
            AdminName = existingAdmin.AdminName
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

        return Ok(new { Token = tokenString });
    }
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
    // -------------------- BookOwner Management --------------------
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpGet("ManageBookOwners")]
    public async Task<ActionResult<IEnumerable<BookOwnerDTOResponse>>> GetPendingBookOwners()
    {
        var pendingBookOwners = await _context.BookOwners
            .Where(b => b.RequestStatus == "Pending")
            .Select(b => new BookOwnerDTOResponse
            {
                BookOwnerID= b.BookOwnerID,
                BookOwnerName = b.BookOwnerName,
                ssn = b.ssn,
                RequestStatus = b.RequestStatus,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber
            })
            .ToListAsync();

        return Ok(pendingBookOwners);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    [HttpPut("ProcessBookOwner/{id}")]
    public async Task<IActionResult> ProcessBookOwner(int id, [FromQuery] string action)
    {
        var bookOwner = await _context.BookOwners.FindAsync(id);
        if (bookOwner == null) return NotFound("Book Owner not found.");
        if (bookOwner.RequestStatus != "Pending") return BadRequest("Book Owner request is already processed.");

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

        _context.Entry(bookOwner).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok($"Book Owner {action}d.");
    }
    [HttpGet("ManageBookPosts")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<BookPostResponseDto>>> GetPendingBookPosts()
    {
        var pendingBookPosts = await _context.BookPosts
            .Where(b => b.PostStatus == "Pending")
            .Include(b => b.BookOwner) // Include BookOwner to access name
            .Select(b => new BookPostResponseDto
            {
                BookOwnerID = b.BookOwnerID,
                BookOwnerName = b.BookOwner.BookOwnerName,
                Title = b.Title,
                Genre = b.Genre,
                ISBN = b.ISBN,
                Description = b.Description,
                Language = b.Language,
                PublicationDate = b.PublicationDate,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Price = b.Price
                // CoverPhoto is not included as it is not stored in the model
            })
            .ToListAsync();

        
        return Ok(pendingBookPosts);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    [HttpPut("ProcessBookPosts/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessBookPosts(int id, [FromQuery] string action)
    {
        var BookPost = await _context.BookPosts.FindAsync(id);
        if (BookPost == null) return NotFound("Book Post not found.");
        if (BookPost.PostStatus != "Pending") return BadRequest("Book Post request is already processed.");

        if (action.ToLower() == "approve")
        {
            BookPost.PostStatus = "Available";
        }
        else if (action.ToLower() == "reject")
        {
            BookPost.PostStatus = "Rejected";
        }
        else
        {
            return BadRequest("Invalid action. Use 'approve' or 'reject'.");
        }

        _context.Entry(BookPost).State = EntityState.Modified;
        await _context.SaveChangesAsync();

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
}
