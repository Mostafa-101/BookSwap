using BookSwap.Data.Contexts;
using BookSwap.Models;
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

    public AdminController(BookSwapDbContext context, IConfiguration configuration)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"]; // المفتاح السري من appsettings.json
    }

    // POST: api/admin/signup
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] Admin admin)
    {
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);

        if (existingAdmin != null)
            return BadRequest("Admin already exists.");

        admin.PasswordHash = HashPassword(admin.PasswordHash); // هاش للباسورد

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return Ok("Admin registered successfully.");
    }

    // POST: api/admin/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Admin admin)
    {
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminName == admin.AdminName);

        if (existingAdmin == null || !VerifyPassword(admin.PasswordHash, existingAdmin.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("name", existingAdmin.AdminName)
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

    // GET: api/admin/{adminName}
    [HttpGet("{adminName}")]
    public async Task<IActionResult> GetAdmin(string adminName)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminName == adminName);

        if (admin == null)
            return NotFound("Admin not found.");

        return Ok(admin);
    }

    // PUT: api/admin/{adminName}
    [HttpPut("{adminName}")]
    public async Task<IActionResult> UpdateAdmin(string adminName, [FromBody] Admin updatedAdmin)
    {
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminName == adminName);

        if (existingAdmin == null)
            return NotFound("Admin not found.");

        // تحديث بيانات الـ Admin
        existingAdmin.AdminName = updatedAdmin.AdminName;
        if (!string.IsNullOrEmpty(updatedAdmin.PasswordHash))
        {
            existingAdmin.PasswordHash = HashPassword(updatedAdmin.PasswordHash); // إعادة هاش للباسورد
        }

        // حفظ التحديثات في قاعدة البيانات
        _context.Admins.Update(existingAdmin);
        await _context.SaveChangesAsync();

        return Ok("Admin updated successfully.");
    }

    // DELETE: api/admin/{adminName}
    [HttpDelete("{adminName}")]
    public async Task<IActionResult> DeleteAdmin(string adminName)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.AdminName == adminName);

        if (admin == null)
            return NotFound("Admin not found.");

        _context.Admins.Remove(admin);
        await _context.SaveChangesAsync();

        return Ok("Admin deleted successfully.");
    }

    // 🔐 هاش للباسورد
    private string HashPassword(string password)
    {
        var key = Encoding.UTF8.GetBytes(_secretKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    // ✅ تحقق من الباسورد
    private bool VerifyPassword(string inputPassword, string storedPasswordHash)
    {
        var inputHash = HashPassword(inputPassword);
        return inputHash == storedPasswordHash;
    }
}
