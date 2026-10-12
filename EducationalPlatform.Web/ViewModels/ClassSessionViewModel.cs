
using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class ClassSessionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a group.")]
        [Display(Name = "Group")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Session date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Session Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        public string Status { get; set; } = "Scheduled";
    }
}