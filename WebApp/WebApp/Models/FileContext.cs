using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
    public class FileContext : DbContext
    {
        public DbSet<FileModel> Files { get; set; }

        public FileContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
