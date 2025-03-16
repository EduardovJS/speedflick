using Microsoft.EntityFrameworkCore;
using SpeedFlick.Models;

namespace SpeedFlick.Context
{
    public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { } 

        public DbSet<UserScore> UserScores { get; set; }


    }
}
