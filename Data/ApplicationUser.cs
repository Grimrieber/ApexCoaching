using Microsoft.AspNetCore.Identity;

namespace OnlineTrainer.Data;

/// <summary>Identity user extended with profile fields shared by clients, trainers and admins.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // One-to-one extensions depending on the user's role.
    public Trainer? TrainerProfile { get; set; }
    public ClientProfile? ClientProfile { get; set; }
}
