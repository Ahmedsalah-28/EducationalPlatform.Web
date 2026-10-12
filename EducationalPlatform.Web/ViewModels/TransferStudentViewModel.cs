using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class TransferStudentViewModel
    {
        public int StudentId { get; set; }

        // Display only
        public string? StudentName { get; set; }

        public int CurrentGroupId { get; set; }

        // Display only
        public string? CurrentYearName { get; set; }

        // Display only
        public string? CurrentGroupName { get; set; }

        [Required(ErrorMessage = "New year is required.")]
        [Display(Name = "New Year")]
        public int NewYearId { get; set; }

        [Required(ErrorMessage = "New group is required.")]
        [Display(Name = "New Group")]
        public int NewGroupId { get; set; }

        [Required(ErrorMessage = "Transfer date is required.")]
        [Display(Name = "Transfer Date")]
        [DataType(DataType.Date)]
        public DateTime TransferDate { get; set; } = DateTime.Today;
    }
}


