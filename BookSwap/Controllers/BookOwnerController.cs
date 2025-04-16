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
[Route("api/bookowner")]
public class BookOwnerController : ControllerBase
{
    private readonly BookSwapDbContext _context;
    private readonly string _secretKey;

    public BookOwnerController(BookSwapDbContext context, IConfiguration configuration)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] BookOwner bookOwner)
    {
        var exists = await _context.BookOwners
            .AnyAsync(b => b.BookOwnerName == bookOwner.BookOwnerName);

        if (exists)
            return BadRequest("BookOwner already exists.");

        bookOwner.Password = HashPassword(bookOwner.Password);
        bookOwner.RequestStatus = "Pending";

        _context.BookOwners.Add(bookOwner);
        await _context.SaveChangesAsync();

        return Ok("BookOwner registered. Waiting for admin approval.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BookOwner bookOwner)
    {
        var existing = await _context.BookOwners
            .FirstOrDefaultAsync(b => b.BookOwnerName == bookOwner.BookOwnerName);

        if (existing == null || !VerifyPassword(bookOwner.Password, existing.Password))
            return Unauthorized("Invalid credentials.");

        if (existing.RequestStatus != "Approved")
            return Forbid("Your account is not approved yet.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("name", existing.BookOwnerName),
                new System.Security.Claims.Claim("role", "BookOwner")
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
                existing.BookOwnerID,
                existing.BookOwnerName,
                existing.Email
            }
        });
    }

    //[Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookOwner>>> GetBookOwners()
    {
        return await _context.BookOwners.ToListAsync();
    }

    //[Authorize(Roles = "BookOwner")]
    [HttpGet("{id}")]
    public async Task<ActionResult<BookOwner>> GetBookOwner(int id)
    {
        var bookOwner = await _context.BookOwners.FindAsync(id);
        if (bookOwner == null)
            return NotFound();
        return bookOwner;
    }

    //[Authorize(Roles = "BookOwner")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBookOwner(int id, [FromBody] BookOwnerDTO updatedOwner)
    {
        var owner = await _context.BookOwners.FindAsync(id);

        if (owner == null)
            return NotFound("BookOwner not found.");

        // Update properties
        owner.BookOwnerName = updatedOwner.BookOwnerName;
        owner.Password = HashPassword(updatedOwner.Password);
        owner.ssn = updatedOwner.ssn;
        owner.RequestStatus = updatedOwner.RequestStatus;
        owner.Email = updatedOwner.Email;
        owner.PhoneNumber = updatedOwner.PhoneNumber;

        _context.BookOwners.Update(owner);
        await _context.SaveChangesAsync();

        return Ok("BookOwner updated successfully.");
    }

    //[Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBookOwner(int id)
    {
        var bookOwner = await _context.BookOwners.FindAsync(id);
        if (bookOwner == null)
            return NotFound();

        _context.BookOwners.Remove(bookOwner);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string HashPassword(string password)
    {
        var key = Encoding.UTF8.GetBytes(_secretKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }
}
