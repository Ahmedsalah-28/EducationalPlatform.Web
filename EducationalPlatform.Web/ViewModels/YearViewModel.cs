using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class YearViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Year name is required.")]
        [StringLength(100)]
        [Display(Name = "Year Name")]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
