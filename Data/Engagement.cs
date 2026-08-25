namespace OnlineTrainer.Data;

/// <summary>An active coaching relationship: a client subscribed to a trainer's package.</summary>
public class Engagement
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public int ServicePackageId { get; set; }
    public ServicePackage? ServicePackage { get; set; }

    public EngagementStatus Status { get; set; } = EngagementStatus.PendingPayment;

    public decimal MonthlyPrice { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    /// <summary>Stripe Subscription id (sub_...) when billed through Stripe.</summary>
    public string? StripeSubscriptionId { get; set; }
}
