using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>A one-off session a client books (and pays for) with a trainer.</summary>
public class Booking
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 60;

    public BookingType Type { get; set; } = BookingType.VideoCall;

    public BookingStatus Status { get; set; } = BookingStatus.Requested;

    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
