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

            //"Host=db;Port=5432;Database=WebDevDB;Username=postgres;Password=pass"
            //"Host=localhost;Port=5432;Database=DB1;Username=postgres;Password=1212"
        }

        public void DeleteFilesToConvertByUserName(string UserName)
        {
            FilesToConvert.RemoveRange(FilesToConvert.Where(f => f.UserName == UserName));
            SaveChanges();
        }

        public void DeleteConvertedFilesByUserName(string UserName)
        {
            ConvertedFiles.RemoveRange(ConvertedFiles.Where(f => f.UserName == UserName));
            SaveChanges();
        }

        public void DeleteFilesToConvertByUserNameAndFullPath(string UserName, string FullPath)
        {
            FilesToConvert.RemoveRange(FilesToConvert.Where(f => f.UserName == UserName && f.FullPath == FullPath));
            SaveChanges();
        }
        public void DeleteConvertedFilesByUserNameAndFullPath(string UserName, string FullPath)
        {
            ConvertedFiles.RemoveRange(ConvertedFiles.Where(f => f.UserName == UserName && f.FullPath == FullPath));
            SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileToConvert>()
                .HasOne(f => f.User)
                .WithMany(u => u.FilesToConvert)
                .HasForeignKey(f => f.UserId);

            modelBuilder.Entity<ConvertedFile>()
                .HasOne(f => f.User)
                .WithMany(u => u.ConvertedFiles)
                .HasForeignKey(f => f.UserId);
        }
    }
}
