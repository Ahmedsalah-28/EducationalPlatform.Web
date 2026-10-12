using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EducationalPlatform.ViewModels
{
    // Training definition (create / edit)
    public class TrainingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string Type { get; set; } = "InClass";

        [Required(ErrorMessage = "Maximum grade is required.")]
        [Range(1, 999.99, ErrorMessage = "Maximum grade must be between 1 and 999.99.")]
        [Display(Name = "Maximum Grade")]
        public decimal MaximumGrade { get; set; } = 10;
    }

    // Assign a training to a group
    public class AssignTrainingViewModel
    {
        public int TrainingId { get; set; }

        public string? TrainingTitle { get; set; }

        public string? TrainingType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Year is required.")]
        [Display(Name = "Year")]
        public int YearId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Group is required.")]
        [Display(Name = "Group")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; } = DateTime.Today;

        [Display(Name = "Start")]
        public DateTime? StartDateTime { get; set; }

        [Display(Name = "End")]
        public DateTime? EndDateTime { get; set; }

        public List<SelectListItem> Years { get; set; } = new();

        public List<SelectListItem> Groups { get; set; } = new();
    }

    // One row in the "Assignments" list
    public class TrainingAssignmentListItem
    {
        public int Id { get; set; }
        public string? TrainingTitle { get; set; }
        public string? TrainingType { get; set; }
        public string? YearName { get; set; }
        public string? GroupName { get; set; }
        public DateTime AssignedDate { get; set; }
        public decimal MaxGrade { get; set; }
        public int GradedCount { get; set; }
        public int StudentCount { get; set; }
    }

    // Grading screen
    public class GradeTrainingViewModel
    {
        public int AssignmentId { get; set; }
        public string? TrainingTitle { get; set; }
        public string? YearName { get; set; }
        public string? GroupName { get; set; }
        public DateTime AssignedDate { get; set; }
        public decimal MaxGrade { get; set; }

        public List<GradeStudentItem> Students { get; set; } = new();
    }

    public class GradeStudentItem
    {
        public int StudentId { get; set; }
        public string? FullName { get; set; }

        // Empty = not graded
        public decimal? Score { get; set; }
    }
}
