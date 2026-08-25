namespace OnlineTrainer.Data;

/// <summary>A money movement on the platform, recording the platform's cut for marketplace accounting.</summary>
public class Payment
{
    public int Id { get; set; }

    public int? ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public PaymentType Type { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Gross amount the client paid.</summary>
    public decimal Amount { get; set; }

    /// <summary>Platform commission (your cut), routed as a Stripe application fee.</summary>
    public decimal PlatformFee { get; set; }

    /// <summary>What the trainer nets after the platform fee.</summary>
    public decimal TrainerNet { get; set; }

    public string? StripeReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
