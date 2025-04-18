using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/admin")]
//[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly BookSwapDbContext _context;
    private readonly string _secretKey;

    public AdminController(BookSwapDbContext context, IConfiguration configuration)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
    }

    // -------------------- Auth Operations --------------------

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] Admin admin)
    {
        var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);
        if (existingAdmin != null) return BadRequest("Admin already exists.");

        admin.PasswordHash = HashPassword(admin.PasswordHash);
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return Ok("Admin registered successfully.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Admin admin)
    {
        var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);
        if (existingAdmin == null || !VerifyPassword(admin.PasswordHash, existingAdmin.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

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
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { Token = tokenString });
    }

    // -------------------- Admin CRUD --------------------

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

    // -------------------- BookOwner Management --------------------

    [HttpGet("ManageBookOwners")]
    public async Task<ActionResult<IEnumerable<BookOwnerDTOResponse>>> GetPendingBookOwners()
    {
        var pendingBookOwners = await _context.BookOwners
            .Where(b => b.RequestStatus == "Pending")
            .Select(b => new BookOwnerDTOResponse
            {
                BookOwnerName = b.BookOwnerName,
                ssn = b.ssn,
                RequestStatus = b.RequestStatus,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber
            })
            .ToListAsync();

        if (pendingBookOwners == null || !pendingBookOwners.Any())
            return NotFound("No pending Book Owners.");

        return Ok(pendingBookOwners);
    }
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
    public async Task<ActionResult<IEnumerable<BookPostDTO>>> GetPendingBookPosts()
    {
        var pendingBookPosts = await _context.BookPosts
            .Where(b => b.PostStatus == "Pending")
            .Select(b => new BookPostDTO
            {
                BookOwnerID = b.BookOwnerID,
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

        if (pendingBookPosts == null || !pendingBookPosts.Any())
            return NotFound("No pending Book Posts.");

        return Ok(pendingBookPosts);
    }
    [HttpPut("ProcessBookPosts/{id}")]
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

    private bool VerifyPassword(string inputPassword, string storedPasswordHash)
    {
        var inputHash = HashPassword(inputPassword);
        return inputHash == storedPasswordHash;
    }
}
