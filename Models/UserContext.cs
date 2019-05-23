using Microsoft.EntityFrameworkCore;
using SERP.Entities;

namespace DbContext
{
    public class UserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        static int Main() => 0;

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseNpgsql("Server=127.0.0.1;Port=5432;Database=SERP;Integrated Security=true;User Id=postgres;Password=q4z4732s;");
            optionsBuilder.UseSqlServer("Server = (localdb)\\mssqllocaldb; Database = DbSERP; Trusted_Connection = True;");
        }
    }
}
