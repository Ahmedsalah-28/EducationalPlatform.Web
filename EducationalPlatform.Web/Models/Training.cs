namespace EducationalPlatform.Models
{
    public class Training
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string Type { get; set; } = null!;

        public decimal MaximumGrade { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedByUserId { get; set; } = null!;

        // Navigation
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<TrainingAssignment> TrainingAssignments { get; set; }
            = new List<TrainingAssignment>();
    }
}
