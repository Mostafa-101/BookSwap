using Microsoft.EntityFrameworkCore;
using BookSwap.Models;

namespace BookSwap.Data.Contexts
{
    public class BookSwapDbContext : DbContext
    {
        public BookSwapDbContext(DbContextOptions<BookSwapDbContext> dbContextOptions) : base(dbContextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Reader> Readers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<BookOwner> BookOwners { get; set; }
        public DbSet<BookPost> BookPosts { get; set; }
        public DbSet<BookRequest> BookRequests { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Reply> Replies { get; set; }


    }
}
