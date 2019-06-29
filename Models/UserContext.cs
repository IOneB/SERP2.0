using Microsoft.EntityFrameworkCore;
using SERP.Entities;

namespace DbContext
{
    public class UserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        static int Main() => 0;

        public DbSet<User> Users { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server = (localdb)\\mssqllocaldb; Database = DbSERPtest; Trusted_Connection = True;");
        }
    }
}
