
using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class GroupScheduleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a group.")]
        [Display(Name = "Group")]
        public int GroupId { get; set; }

        [Required]
        [Display(Name = "Day")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}