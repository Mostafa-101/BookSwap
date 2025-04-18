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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/bookowner")]
public class BookOwnerController : ControllerBase
{
    private readonly BookSwapDbContext _context;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _Audience;

    public BookOwnerController(BookSwapDbContext context, IConfiguration configuration)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
        _issuer = configuration["jwt:Issuer"];
        _Audience = configuration["jwt:Audience"];
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
            Password = PasswordService.HashPassword(bookOwnerDto.Password),
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

        if (existing == null || !PasswordService.VerifyPassword(bookOwnerDTO.Password, existing.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Check account status
        if (existing.RequestStatus == "Pending")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account is pending approval." });
        }
        if (existing.RequestStatus != "Approved")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account is not approved." });
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
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _issuer,
            Audience = _Audience
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
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> CreateBookPost([FromForm] BookPostDTO dto)
    {
        using var stream = new MemoryStream();
        await dto.CoverPhoto.CopyToAsync(stream);
        var post = new BookPost
        {
            BookOwnerID = dto.BookOwnerID,
            Title = dto.Title,
            Genre = dto.Genre,
            ISBN = dto.ISBN,
            Description = dto.Description,
            Language = dto.Language,
            PublicationDate = dto.PublicationDate,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Price = dto.Price,
            PostStatus = "Pending",
            CoverPhoto = stream.ToArray()
        };
        await _context.BookPosts.AddAsync(post);
        await _context.SaveChangesAsync();
        return Ok("Book post created successfully!");
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("{id}")]

    public async Task<IActionResult> DeleteBookPost(int id)
    {
        var post = await _context.BookPosts.FindAsync(id);

        if (post == null)
        {
            return NotFound($"No BookPost found with ID = {id}");
        }

        if (post.PostStatus == "Borrowed")
        {
            return BadRequest("Cannot delete a borrowed book post.");
        }

        _context.BookPosts.Remove(post);
        await _context.SaveChangesAsync();

        return Ok($"BookPost with ID = {id} deleted successfully.");
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> UpdateBookPost(int id, [FromForm] BookPostDTO dto)
    {
        var post = await _context.BookPosts.FindAsync(id);
        using var stream = new MemoryStream();
        await dto.CoverPhoto.CopyToAsync(stream);
        if (post == null)
            return NotFound($"BookPost with ID = {id} not found.");

        post.Title = dto.Title;
        post.Genre = dto.Genre;
        post.ISBN = dto.ISBN;
        post.Description = dto.Description;
        post.Language = dto.Language;
        post.PublicationDate = dto.PublicationDate;
        post.StartDate = dto.StartDate;
        post.EndDate = dto.EndDate;
        post.Price = dto.Price;
        post.CoverPhoto = stream.ToArray();



        await _context.SaveChangesAsync();

        return Ok($"BookPost with ID = {id} updated successfully.");
    }
    // //[Authorize(Roles = "Admin")]
    // [HttpGet]
    // public async Task<ActionResult<IEnumerable<BookOwner>>> GetBookOwners()
    // {
    //     return await _context.BookOwners.ToListAsync();
    // }

    // //[Authorize(Roles = "BookOwner")]
    // [HttpGet("{id}")]
    // public async Task<ActionResult<BookOwner>> GetBookOwner(int id)
    // {
    //     var bookOwner = await _context.BookOwners.FindAsync(id);
    //     if (bookOwner == null)
    //         return NotFound();
    //     return bookOwner;
    // }

    // //[Authorize(Roles = "BookOwner")]
    ///* [HttpPut("{id}")]
    // public async Task<IActionResult> UpdateBookOwner(int id, [FromBody] BookOwnerSignUpDTO updatedOwner)
    // {
    //     var owner = await _context.BookOwners.FindAsync(id);

    //     if (owner == null)
    //         return NotFound("BookOwner not found.");

    //     // Update properties
    //     owner.BookOwnerName = updatedOwner.BookOwnerName;
    //     owner.Password = HashPassword(updatedOwner.Password);
    //     owner.ssn = updatedOwner.ssn;
    //     owner.RequestStatus = updatedOwner.RequestStatus;
    //     owner.Email = updatedOwner.Email;
    //     owner.PhoneNumber = updatedOwner.PhoneNumber;

    //     _context.BookOwners.Update(owner);
    //     await _context.SaveChangesAsync();

    //     return Ok("BookOwner updated successfully.");
    // }*/

    // //[Authorize(Roles = "Admin")]
    // [HttpDelete("{id}")]
    // public async Task<IActionResult> DeleteBookOwner(int id)
    // {
    //     var bookOwner = await _context.BookOwners.FindAsync(id);
    //     if (bookOwner == null)
    //         return NotFound();

    //     _context.BookOwners.Remove(bookOwner);
    //     await _context.SaveChangesAsync();

    //     return NoContent();
    // }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("respond")]
    [Authorize(Roles = "BookOwner")]

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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("owner/")]
    [Authorize(Roles = "BookOwner")]

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

  /*  private string HashPassword(string password)
    {
        var key = Encoding.UTF8.GetBytes(_secretKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }*/
}
