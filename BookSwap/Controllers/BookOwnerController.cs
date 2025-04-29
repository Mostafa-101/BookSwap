using BookSwap.Controllers;
using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using BookSwap.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/bookowner")]
public class BookOwnerController : ControllerBase
{
    private readonly BookSwapDbContext _context;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _Audience;
    private readonly GenericRepo<BookPost> _bookPostRepo;
    private readonly GenericRepo<BookOwner> _BookOwnerRepo;
    private readonly GenericRepo<BookRequest> _bookRequestRepo;
    private readonly GenericRepo<RefreshToken> _refreshTokenRepo;

    public BookOwnerController(
        BookSwapDbContext context,
        IConfiguration configuration,
        GenericRepo<BookPost> bookPostRepo,
        GenericRepo<BookOwner> BookOwnerRepo,
        GenericRepo<BookRequest> bookRequestRepo,
        GenericRepo<RefreshToken> refreshTokenRepo)
    {
        _context = context;
        _secretKey = configuration["Jwt:Key"];
        _issuer = configuration["jwt:Issuer"];
        _Audience = configuration["jwt:Audience"];
        _bookPostRepo = bookPostRepo;
        _BookOwnerRepo = BookOwnerRepo;
        _bookRequestRepo = bookRequestRepo;
        _refreshTokenRepo = refreshTokenRepo;
    }

    // Helper method to verify bookOwnerId from JWT
    private IActionResult VerifyBookOwnerId(int bookOwnerId)
    {
        var jwtBookOwnerId = User.FindFirst("bookOwnerId")?.Value;
        if (jwtBookOwnerId == null || !int.TryParse(jwtBookOwnerId, out int parsedJwtBookOwnerId) || parsedJwtBookOwnerId != bookOwnerId)
        {
            return Unauthorized(new { message = "You are not authorized to access this resource" });
        }
        return null; // Indicates verification passed
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] BookOwnerSignUpDTO bookOwnerDto)
    {
        var exists = (await _BookOwnerRepo.getAllFilterAsync(b => b.BookOwnerName == bookOwnerDto.BookOwnerName)).Any();
        if (exists)
            return BadRequest("BookOwner already exists.");

        var bookOwner = new BookOwner
        {
            BookOwnerName = bookOwnerDto.BookOwnerName,
            Password = PasswordService.HashPassword(bookOwnerDto.Password),
            EncryptedSsn = PasswordService.Encrypt(bookOwnerDto.ssn),
            RequestStatus = "Pending",
            EncryptedEmail = PasswordService.Encrypt(bookOwnerDto.Email),
            EncryptedPhoneNumber = PasswordService.Encrypt(bookOwnerDto.PhoneNumber)
        };

        var added = _BookOwnerRepo.add(bookOwner);
        if (!added)
            return StatusCode(500, "Failed to register BookOwner.");

        return Ok("BookOwner registered. Waiting for admin approval.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BookOwnerLoginDTO bookOwnerDTO)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingList = await _BookOwnerRepo.getAllFilterAsync(b => b.BookOwnerName == bookOwnerDTO.BookOwnerName);
        var existing = existingList.FirstOrDefault();

        if (existing == null || !PasswordService.VerifyPassword(bookOwnerDTO.Password, existing.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        if (existing.RequestStatus == "Pending")
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account is pending approval." });

        if (existing.RequestStatus != "Approved")
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account is not approved." });

        var tokenString = PasswordService.GenerateJwtToken(
            _secretKey,
            _issuer,
            _Audience,
            existing.BookOwnerName,
            "BookOwner",
            existing.BookOwnerID.ToString()
        );

        var refreshToken = PasswordService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            UserId = existing.BookOwnerID.ToString(),
            UserType = "BookOwner",
            BookOwnerId = existing.BookOwnerID
        };

        bool isRefreshTokenAdded = _refreshTokenRepo.add(refreshTokenEntity);
        if (!isRefreshTokenAdded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to save refresh token." });

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
                existing.BookOwnerID,
                existing.BookOwnerName,
                Email = existing.GetDecryptedEmail(),
                PhoneNumber = existing.GetDecryptedPhoneNumber()
            }
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> CreateBookPost([FromForm] BookPostDTO dto)
    {
        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(dto.BookOwnerID);
        if (verificationResult != null)
            return verificationResult;

        // Verify book owner exists
        var bookOwner = _BookOwnerRepo.getById(dto.BookOwnerID);
        if (bookOwner == null)
            return NotFound(new { message = "Book owner not found" });

        // Convert CoverPhoto to byte array
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

        bool isAdded = _bookPostRepo.add(post);
        if (!isAdded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to create book post." });

        return Ok(new { message = "Book post created successfully!" });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBookPost(int id)
    {
        var post = _bookPostRepo.getById(id);
        if (post == null)
            return NotFound($"No BookPost found with ID = {id}");

        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(post.BookOwnerID);
        if (verificationResult != null)
            return verificationResult;

        if (post.PostStatus == "Borrowed")
            return BadRequest("Cannot delete a borrowed book post.");

        bool isDeleted = _bookPostRepo.remove(id);
        if (!isDeleted)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to delete BookPost with ID = {id}." });

        return Ok($"BookPost with ID = {id} deleted successfully.");
    }

    [HttpPut("UpdateBookPost/{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> UpdateBookPost(int id, [FromForm] BookPostDTO dto)
    {
        var post = _bookPostRepo.getById(id);
        if (post == null)
            return NotFound($"BookPost with ID = {id} not found.");

        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(post.BookOwnerID);
        if (verificationResult != null)
            return verificationResult;

        using var stream = new MemoryStream();
        await dto.CoverPhoto.CopyToAsync(stream);

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

        bool isUpdated = _bookPostRepo.update(post);
        if (!isUpdated)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to update BookPost with ID = {id}." });

        return Ok($"BookPost with ID = {id} updated successfully.");
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "BookOwner")]
    [HttpPut("UpdateBookOwner/{id}")]
    public async Task<IActionResult> UpdateBookOwner(int id, [FromBody] UpdateBookOwnerDTO updatedOwner)
    {
        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(id);
        if (verificationResult != null)
            return verificationResult;

        var existingOwner = _BookOwnerRepo.getById(id);
        if (existingOwner == null)
            return NotFound("BookOwner not found.");

        existingOwner.BookOwnerName = updatedOwner.BookOwnerName ?? existingOwner.BookOwnerName;
        existingOwner.Password = string.IsNullOrEmpty(updatedOwner.Password)
            ? existingOwner.Password
            : PasswordService.HashPassword(updatedOwner.Password);
        existingOwner.EncryptedSsn = string.IsNullOrEmpty(updatedOwner.ssn)
            ? existingOwner.EncryptedSsn
            : PasswordService.Encrypt(updatedOwner.ssn);
        existingOwner.EncryptedEmail = updatedOwner.Email != null
            ? PasswordService.Encrypt(updatedOwner.Email)
            : existingOwner.EncryptedEmail;
        existingOwner.EncryptedPhoneNumber = updatedOwner.PhoneNumber != null
            ? PasswordService.Encrypt(updatedOwner.PhoneNumber)
            : existingOwner.EncryptedPhoneNumber;

        bool isUpdated = _BookOwnerRepo.update(existingOwner);
        if (!isUpdated)
            return StatusCode(500, "Failed to update BookOwner.");

        return Ok("BookOwner updated successfully.");
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("respond")]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> RespondToBookRequest([FromBody] BookRequestResponseDTO responseDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var bookRequest = (await _bookRequestRepo.getAllFilterAsync(
            filter: br => br.RequsetID == responseDto.RequsetID,
            include: q => q.Include(br => br.BookPost)
                          .ThenInclude(bp => bp.BookOwner)
        )).FirstOrDefault();

        if (bookRequest == null)
            return NotFound(new { message = "Book request not found" });

        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(bookRequest.BookPost.BookOwnerID);
        if (verificationResult != null)
            return verificationResult;

        if (bookRequest.BookPostID != responseDto.BookPostID || bookRequest.ReaderID != responseDto.ReaderID)
            return BadRequest(new { message = "Book post or reader ID mismatch" });

        if (bookRequest.BookPost.PostStatus.ToLower() == "borrowed")
            return BadRequest(new { message = "Book is already borrowed" });

        var validStatuses = new[] { "Accepted", "Rejected" };
        if (!validStatuses.Contains(responseDto.RequsetStatus))
            return BadRequest(new { message = "Invalid request status. Must be 'Accepted' or 'Rejected'" });

        bookRequest.RequsetStatus = responseDto.RequsetStatus;
        bookRequest.BookPost.PostStatus = responseDto.RequsetStatus == "Accepted" ? "Borrowed" : "Available";

        try
        {
            bool requestUpdated = _bookRequestRepo.update(bookRequest);
            bool postUpdated = _bookPostRepo.update(bookRequest.BookPost);

            if (requestUpdated && postUpdated)
                return Ok(new
                {
                    message = $"Book request {responseDto.RequsetStatus.ToLower()} successfully",
                    requestId = bookRequest.RequsetID,
                    bookPostId = bookRequest.BookPostID
                });
            else
                return StatusCode(500, new { message = "Error updating request or post" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error processing request response", error = ex.Message });
        }
    }

    [HttpGet("owner/")]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> GetBookRequestsForOwner(int bookOwnerId)
    {
        var verificationResult = VerifyBookOwnerId(bookOwnerId);
        if (verificationResult != null)
            return verificationResult;

        var bookOwner = _BookOwnerRepo.getById(bookOwnerId);
        if (bookOwner == null)
            return NotFound(new { message = "Book owner not found" });

        var bookRequests = await _bookRequestRepo.getAllFilterAsync(
            filter: br => br.BookPost.BookOwnerID == bookOwnerId,
            include: q => q.Include(br => br.BookPost)
                          .Include(br => br.Reader)
        );

        var bookRequestDtos = bookRequests.Select(br => new GetBookRequestsDTO
        {
            RequsetID = br.RequsetID,
            BookPostID = br.BookPostID,
            BookTitle = br.BookPost.Title,
            ReaderID = br.ReaderID,
            ReaderName = br.Reader.ReaderName,
            RequsetStatus = br.RequsetStatus
        }).ToList();

        if (!bookRequestDtos.Any())
            return Ok(new { message = "No book requests found for this owner", requests = new List<GetBookRequestsDTO>() });

        return Ok(new
        {
            message = "Book requests retrieved successfully",
            requests = bookRequestDtos
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("posts/{bookOwnerId}")]
    [Authorize(Roles = "BookOwner")]
    public async Task<IActionResult> GetAllPostsForOwner(int bookOwnerId)
    {
        // Verify bookOwnerId from JWT
        var verificationResult = VerifyBookOwnerId(bookOwnerId);
        if (verificationResult != null)
            return verificationResult;

        var bookOwner = _BookOwnerRepo.getById(bookOwnerId);
        if (bookOwner == null)
            return NotFound(new { message = "Book owner not found" });

        var bookPosts = await _bookPostRepo.getAllFilterAsync(
            filter: bp => bp.BookOwnerID == bookOwnerId,
            include: q => q.Include(bp => bp.BookOwner)
                          .Include(bp => bp.Likes)
        );

        var bookPostDtos = bookPosts.Select(bp => new BookPostResponseDto
        {
            BookOwnerID = bp.BookOwnerID,
            BookOwnerName = bp.BookOwner.BookOwnerName,
            BookPostID = bp.BookPostID,
            Title = bp.Title,
            Genre = bp.Genre,
            ISBN = bp.ISBN,
            Description = bp.Description,
            Language = bp.Language,
            PublicationDate = bp.PublicationDate,
            StartDate = bp.StartDate,
            EndDate = bp.EndDate,
            Price = bp.Price,
            CoverPhoto = bp.CoverPhoto != null ? Convert.ToBase64String(bp.CoverPhoto) : null,
            TotalLikes = bp.Likes.Count(l => l.IsLike),
            TotalDislikes = bp.Likes.Count(l => !l.IsLike)
        }).ToList();

        if (!bookPostDtos.Any())
            return Ok(new { message = "No book posts found for this owner", posts = new List<BookPostResponseDto>() });

        return Ok(new
        {
            message = "Book posts retrieved successfully",
            posts = bookPostDtos
        });
    }
}