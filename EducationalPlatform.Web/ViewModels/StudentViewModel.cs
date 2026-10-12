using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        public string? Phone { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Year is required.")]
        [Display(Name = "Year")]
        public int YearId { get; set; }

        [Required(ErrorMessage = "Group is required.")]
        [Display(Name = "Group")]
        public int GroupId { get; set; }

        [Display(Name = "Joined At")]
        [DataType(DataType.Date)]
        public DateTime JoinedAt { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}


