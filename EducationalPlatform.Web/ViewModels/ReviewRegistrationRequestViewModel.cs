
using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class ReviewRegistrationRequestViewModel
    {
        public int RequestId { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? GroupName { get; set; }

        [Required(ErrorMessage = "Initial password is required.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Initial Password")]
        public string Password { get; set; } = null!;
    }
}
