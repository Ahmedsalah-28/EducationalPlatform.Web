using Microsoft.AspNetCore.Identity;

namespace EducationalPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Student? Student { get; set; }

        public Assistant? Assistant { get; set; }
    }
}
