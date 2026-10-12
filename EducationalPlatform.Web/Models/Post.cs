namespace EducationalPlatform.Models
{
    public class Post
    {
        public int Id { get; set; }

        public int YearId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public string CreatedByUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = true;


        public Year Year { get; set; } = null!;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<PostAttachment> Attachments { get; set; }
            = new List<PostAttachment>();
    }
}