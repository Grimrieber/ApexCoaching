using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>A coach offering services on the platform. The marketplace is built around this entity.</summary>
public class Trainer
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>URL-friendly identifier used on the public profile, e.g. /trainers/alex-rivera.</summary>
    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Headline { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>Comma-separated specialties (e.g. "Strength, Fat loss, Mobility").</summary>
    [MaxLength(300)]
    public string Specialties { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Location { get; set; }

    public int YearsExperience { get; set; }

    /// <summary>Per-session rate for one-off bookings.</summary>
    public decimal HourlyRate { get; set; }

    /// <summary>Platform commission taken from this trainer's revenue (0.20 = 20%).</summary>
    public decimal CommissionRate { get; set; } = 0.20m;

    /// <summary>Stripe Connect account id (acct_...). Null until the trainer onboards.</summary>
    public string? StripeAccountId { get; set; }

    public bool StripeOnboarded { get; set; }

    /// <summary>Admin approval gate before a trainer is shown in the marketplace.</summary>
    public bool IsApproved { get; set; }

    public bool IsAcceptingClients { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ServicePackage> Packages { get; set; } = new List<ServicePackage>();
    public ICollection<ClientProfile> Clients { get; set; } = new List<ClientProfile>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public IEnumerable<string> SpecialtyList =>
        Specialties.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
