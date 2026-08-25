using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>A prospect captured from the public contact form (lead capture).</summary>
public class Lead
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Goal { get; set; }

    /// <summary>Optional: the trainer this lead came in for (deep-linked from a profile).</summary>
    public int? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
