using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class FileAlreadyConverted
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string PathToFolder { get; set; }

        [Required]
        public string FullPathToFile { get; set; }
    }
}
