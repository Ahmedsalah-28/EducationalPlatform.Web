
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EducationalPlatform.ViewModels
{
    public class AttendanceSessionViewModel
    {
        public int SessionId { get; set; }

        public string YearName { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string SessionStatus { get; set; } = string.Empty;

        public List<AttendanceStudentViewModel> Students { get; set; }
            = new List<AttendanceStudentViewModel>();
    }

    public class AttendanceStudentViewModel
    {
        public int StudentId { get; set; }

        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Present";
    }
}