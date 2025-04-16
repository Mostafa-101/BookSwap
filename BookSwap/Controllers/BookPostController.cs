using BookSwap.Data.Contexts;
using BookSwap.DTOS;
using BookSwap.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace BookSwap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookPostController : ControllerBase
    {
        private readonly BookSwapDbContext _db;

        public BookPostController(BookSwapDbContext db)
        {
            _db = db;
        }
        [HttpPost]
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
                PostStatus="Pending",
                CoverPhoto =stream.ToArray()
            };
            await _db.BookPosts.AddAsync(post);
            await _db.SaveChangesAsync();
            return Ok("Book post created successfully!");
        }
        [HttpGet("Available")]
        public async Task<IActionResult> GetAvailableBookPosts()
        {
            var acceptedPosts = await _db.BookPosts
                .Where(bp => bp.PostStatus == "Available")
                .Select(bp => new BookPostResponseDTO
                {
                    BookOwnerID = bp.BookOwnerID,
                    Title = bp.Title,
                    Genre = bp.Genre,
                    ISBN = bp.ISBN,
                    Description = bp.Description,
                    Language = bp.Language,
                    PublicationDate = bp.PublicationDate,
                    StartDate = bp.StartDate,
                    EndDate = bp.EndDate,
                    Price = bp.Price,
                    CoverPhotoBase64 = Convert.ToBase64String(bp.CoverPhoto)
                })
                .ToListAsync();

            return Ok(acceptedPosts);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookPost(int id)
        {
            var post = await _db.BookPosts.FindAsync(id);

            if (post == null)
            {
                return NotFound($"No BookPost found with ID = {id}");
            }

            _db.BookPosts.Remove(post);
            await _db.SaveChangesAsync();

            return Ok($"BookPost with ID = {id} deleted successfully.");
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookPost(int id, [FromForm] BookPostDTO dto)
        {
            var post = await _db.BookPosts.FindAsync(id);
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



            await _db.SaveChangesAsync();

            return Ok($"BookPost with ID = {id} updated successfully.");
        }
    }
}
