using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>A monthly coaching package offered by a trainer.</summary>
public class ServicePackage
{
    public int Id { get; set; }

    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public PackageTier Tier { get; set; }

    [MaxLength(600)]
    public string Description { get; set; } = string.Empty;

    public decimal PriceMonthly { get; set; }

    /// <summary>Newline-separated feature bullets shown on pricing cards.</summary>
    [MaxLength(1000)]
    public string Features { get; set; } = string.Empty;

    public bool IsPopular { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Stripe Price id (price_...) for recurring billing, when configured.</summary>
    public string? StripePriceId { get; set; }

    public IEnumerable<string> FeatureList =>
        Features.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
