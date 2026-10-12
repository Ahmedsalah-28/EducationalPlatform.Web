namespace EducationalPlatform.Models
{
    public class AssistantYear
    {
        public int AssistantId { get; set; }

        public int YearId { get; set; }

        // Navigation
        public Assistant Assistant { get; set; } = null!;

        public Year Year { get; set; } = null!;
    }
}