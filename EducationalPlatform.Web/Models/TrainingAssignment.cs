namespace EducationalPlatform.Models
{
    public class TrainingAssignment
    {
        public int Id { get; set; }

        public int TrainingId { get; set; }

        public int GroupId { get; set; }

        public DateTime AssignedDate { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        // Navigation
        public Training Training { get; set; } = null!;

        public Group Group { get; set; } = null!;

        public ICollection<TrainingRecord> TrainingRecords { get; set; }
            = new List<TrainingRecord>();

        public ICollection<TrainingAttempt> TrainingAttempts { get; set; }
            = new List<TrainingAttempt>();
    }
}
