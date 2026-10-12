namespace EducationalPlatform.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int GroupId { get; set; }

        public DateTime JoinedAt { get; set; }

        public DateTime? LeftAt { get; set; }

        public string Status { get; set; } = "Active";

        // Navigation
        public Student Student { get; set; } = null!;

        public Group Group { get; set; } = null!;
    }
}