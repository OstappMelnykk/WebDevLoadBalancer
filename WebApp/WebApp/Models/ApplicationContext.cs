using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
	public class ApplicationContext : IdentityDbContext<User>
	{
        public DbSet<FileToConvetModel> FilesToConvet { get; set; }
        public DbSet<FileAlreadyConverted> FilesAlreadyConverted { get; set; }


        public ApplicationContext(DbContextOptions<ApplicationContext> options)
			: base(options)
		{
			Database.EnsureCreated();
		}

        public void DeleteFromFilesToConvet()
        {
            Database.ExecuteSqlRaw("DELETE FROM PUBLIC.\"FilesToConvet\"");
        }

        public void DeleteFromFilesAlreadyConverted()
        {
            Database.ExecuteSqlRaw("DELETE FROM PUBLIC.\"FilesAlreadyConverted\"");
        }
    }
}
