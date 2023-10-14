using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class FileModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string FilePath { get; set; }
    }
}
