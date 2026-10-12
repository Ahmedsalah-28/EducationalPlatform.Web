namespace EducationalPlatform.Models
{
    public class PostAttachment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public string FileName { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public string? ContentType { get; set; }

        public long FileSize { get; set; }


        public Post Post { get; set; } = null!;
    }
}
