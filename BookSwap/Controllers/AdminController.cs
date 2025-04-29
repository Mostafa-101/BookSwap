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
        [HttpPost("admin/login")]
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

            var tokenString = PasswordService.GenerateJwtToken(
                _secretKey, 
                _issuer, 
                _Audience, 
                existing.AdminName, 
                "Admin"
                
            );

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
   
}
