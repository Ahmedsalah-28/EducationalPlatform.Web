namespace EducationalPlatform.Models
{
    public class AttendanceSettings
    {
        public int Id { get; set; }

        public decimal WarningPercentage { get; set; }

        public decimal CriticalPercentage { get; set; }

        public int MaximumAbsences { get; set; }
    }
}