using MeniuMate_API.Auth.Model;
using MeniuMate_API.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeniuMate_API.Data
{
    public class ForumDbContext : IdentityDbContext<ForumRestUser>
    {
        private readonly IConfiguration _configuration;
        public DbSet<Meniu> Menius { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public ForumDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
        }
    }
}
