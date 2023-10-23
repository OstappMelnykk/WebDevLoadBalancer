using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class FileToConvert
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Path { get; set; }
        [Required]
        public string FullPath { get; set; }

        [Required]
        public string UserName { get; set; }
        [Required]
        public string UserId { get; set; }
        public User User { get; set; } // Foreign key relationship
    }
}
