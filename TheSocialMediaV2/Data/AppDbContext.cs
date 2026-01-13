using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Entities;

namespace TheSocialMediaV2.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AdminActionLog> AdminActionLogs { get; set; }
    }
}