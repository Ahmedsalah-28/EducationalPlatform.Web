namespace EducationalPlatform.Models
{
    public class RegistrationRequest
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        public int RequestedGroupId { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedByUserId { get; set; }


        public Group RequestedGroup { get; set; } = null!;

        public ApplicationUser? ReviewedByUser { get; set; }
    }
}
