
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EducationalPlatform.ViewModels
{
    public class PostCreateViewModel
    {
        [Required]
        public int YearId { get; set; }

        [Required(ErrorMessage = "Please enter the post title.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the post content.")]
        public string Content { get; set; } = string.Empty;

        public List<IFormFile> Files { get; set; } = new();
    }
}