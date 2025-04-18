using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    public async Task<IActionResult> SignUp([FromBody] BookOwnerSignUpDTO bookOwnerDto)
    {
        var exists = await _context.BookOwners
            .AnyAsync(b => b.BookOwnerName == bookOwnerDto.BookOwnerName);

        if (exists)
            return BadRequest("BookOwner already exists.");

        var bookOwner = new BookOwner
        {
            BookOwnerName = bookOwnerDto.BookOwnerName,
            Password = HashPassword(bookOwnerDto.Password),
            ssn = bookOwnerDto.ssn,
            RequestStatus = "Pending",
            Email = bookOwnerDto.Email,
            PhoneNumber = bookOwnerDto.PhoneNumber
        };

        _context.BookOwners.Add(bookOwner);
        await _context.SaveChangesAsync();

        return Ok("BookOwner registered. Waiting for admin approval.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BookOwnerLoginDTO bookOwnerDTO)
    {
        // Validate input
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find book owner by BookOwnerName
        var existing = await _context.BookOwners
            .FirstOrDefaultAsync(b => b.BookOwnerName == bookOwnerDTO.BookOwnerName);

        if (existing == null || !VerifyPassword(bookOwnerDTO.Password, existing.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Check if account is approved
        if (existing.RequestStatus != "Approved")
        {
            return Forbid("Your account is not approved yet.");
        }

        // Generate JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim("name", existing.BookOwnerName),
                    new Claim("role", "BookOwner"),
                    new Claim("bookOwnerId", existing.BookOwnerID.ToString()) // Optional: Include BookOwnerID for other APIs
                }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Return token and book owner details
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
    public async Task<IActionResult> UpdateBookOwner(int id, [FromBody] BookOwnerSignUpDTO updatedOwner)
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
    [HttpPost("respond")]
    public async Task<IActionResult> RespondToBookRequest([FromBody] BookRequestResponseDTO responseDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find the book request
        var bookRequest = await _context.BookRequests
            .Include(br => br.BookPost)
            .ThenInclude(bp => bp.BookOwner)
            .FirstOrDefaultAsync(br => br.RequsetID == responseDto.RequsetID);

        if (bookRequest == null)
        {
            return NotFound(new { message = "Book request not found" });
        }

        // Verify the book post and reader match the DTO
        if (bookRequest.BookPostID != responseDto.BookPostID || bookRequest.ReaderID != responseDto.ReaderID)
        {
            return BadRequest(new { message = "Book post or reader ID mismatch" });
        }

        // Verify the book is not already borrowed
        if (bookRequest.BookPost.PostStatus.ToLower() == "borrowed")
        {
            return BadRequest(new { message = "Book is already borrowed" });
        }

        // Validate request status
        var validStatuses = new[] { "Accepted", "Rejected" };
        if (!validStatuses.Contains(responseDto.RequsetStatus))
        {
            return BadRequest(new { message = "Invalid request status. Must be 'Accepted' or 'Rejected'" });
        }

        // Verify the book owner is authorized (assuming the owner ID is available via authentication)
        // Note: In a real application, you'd get the owner ID from the authenticated user context
        var bookOwnerId = bookRequest.BookPost.BookOwnerID;
        if (bookRequest.BookPost.BookOwnerID != bookOwnerId)
        {
            return Unauthorized(new { message = "Only the book owner can respond to this request" });
        }

        // Update the book request status
        bookRequest.RequsetStatus = responseDto.RequsetStatus;

        // Update book post status based on the response
        bookRequest.BookPost.PostStatus = responseDto.RequsetStatus == "Accepted" ? "Borrowed" : "Available";

        try
        {
            _context.BookRequests.Update(bookRequest);
            _context.BookPosts.Update(bookRequest.BookPost);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Book request {responseDto.RequsetStatus.ToLower()} successfully",
                requestId = bookRequest.RequsetID,
                bookPostId = bookRequest.BookPostID
            });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new { message = "Error processing request response", error = ex.Message });
        }
    }
    [HttpGet("owner/")]
    public async Task<IActionResult> GetBookRequestsForOwner(int bookOwnerId)
    {
        // Verify book owner exists
        var bookOwner = await _context.BookOwners
            .FirstOrDefaultAsync(bo => bo.BookOwnerID == bookOwnerId);

        if (bookOwner == null)
        {
            return NotFound(new { message = "Book owner not found" });
        }

        // Fetch all book requests for books owned by this owner and map to DTO
        var bookRequests = await _context.BookRequests
            .Include(br => br.BookPost)
            .Include(br => br.Reader)
            .Where(br => br.BookPost.BookOwnerID == bookOwnerId)
            .Select(br => new GetBookRequestsDTO
            {
                RequsetID = br.RequsetID,
                BookPostID = br.BookPostID,
                BookTitle = br.BookPost.Title,
                ReaderID = br.ReaderID,
                ReaderName = br.Reader.ReaderName,
                RequsetStatus = br.RequsetStatus
            })
            .ToListAsync();

        if (!bookRequests.Any())
        {
            return Ok(new { message = "No book requests found for this owner", requests = new List<GetBookRequestsDTO>() });
        }

        return Ok(new
        {
            message = "Book requests retrieved successfully",
            requests = bookRequests
        });
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
