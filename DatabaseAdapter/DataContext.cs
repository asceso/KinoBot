using Microsoft.EntityFrameworkCore;
using Models.Database;

namespace DatabaseAdapter
{
    public class DataContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }

        public DataContext()
        {
            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=kibo_bot.db");
        }
    }
}