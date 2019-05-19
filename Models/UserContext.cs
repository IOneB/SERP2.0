using Microsoft.EntityFrameworkCore;
using SERP.Entities;

namespace DbContext
{
    public class UserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        static int Main() => 0;

        public DbSet<User> Users { get; set; }

        public UserContext() : base()
        {
            Database.Migrate();
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DbSERP;Trusted_Connection=True;");
        }
    }
}
