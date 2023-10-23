using Microsoft.AspNetCore.Identity;

namespace WebApp.Models
{
	public class User : IdentityUser {
        public ICollection<FileToConvert> FilesToConvert { get; set; }
        public ICollection<ConvertedFile> ConvertedFiles { get; set; }
    }
}
