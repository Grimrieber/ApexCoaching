using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>Profile attached to a user with the Client role.</summary>
public class ClientProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>The coach this client is working with (null until they engage a trainer).</summary>
    public int? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    [MaxLength(500)]
    public string? Goal { get; set; }

    [MaxLength(60)]
    public string? ExperienceLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Engagement> Engagements { get; set; } = new List<Engagement>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<TrainingProgram> Programs { get; set; } = new List<TrainingProgram>();
}
