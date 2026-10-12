using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class AssistantViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = null!;

        // Required on Create (checked in the controller), read-only on Edit
        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        // Required on Create (checked in the controller),
        // optional on Edit: leave empty to keep the current password
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [Display(Name = "Allowed Years")]
        public List<int> SelectedYearIds { get; set; } = new();

        // Filled by the controller, not posted by the form
        public List<YearOption> AvailableYears { get; set; } = new();
    }

    public class YearOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
