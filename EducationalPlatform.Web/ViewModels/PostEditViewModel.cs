using System.ComponentModel.DataAnnotations;
using EducationalPlatform.Models;
using Microsoft.AspNetCore.Http;

namespace EducationalPlatform.ViewModels
{
    public class PostEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = " Year")]
        public int YearId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Attachments already saved with the post (display only).
        public List<PostAttachment> ExistingAttachments { get; set; } = new();

        // Ids of existing attachments the user ticked for removal.
        public List<int> RemoveAttachmentIds { get; set; } = new();

        // New files to add.
        public List<IFormFile> Files { get; set; } = new();
    }
}