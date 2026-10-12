using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class GroupViewModel
    {
        public int Id { get; set; }

        [Required]
        public int YearId { get; set; }

        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100)]
        [Display(Name = "Group Name")]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}