using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
	public class ApplicationContext : IdentityDbContext<User>
	{
        public DbSet<FileToConvert> FilesToConvert { get; set; }
        public DbSet<ConvertedFile> ConvertedFiles { get; set; }


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
