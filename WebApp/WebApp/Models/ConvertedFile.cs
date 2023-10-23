using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class ConvertedFile
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Path { get; set; }
        [Required]
        public string FullPath { get; set; }
    }
}
