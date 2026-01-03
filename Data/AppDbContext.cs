using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UsersApp.Models;

namespace UsersApp.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<MoodEntry> MoodEntries { get; set; }
    }
}
