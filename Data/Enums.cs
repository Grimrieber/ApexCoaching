namespace OnlineTrainer.Data;

/// <summary>Application role names used across Identity and authorization policies.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Trainer = "Trainer";
    public const string Client = "Client";

    public static readonly string[] All = { Admin, Trainer, Client };
}

public enum PackageTier
{
    Starter = 0,
    Coached = 1,
    Premium = 2
}

public enum EngagementStatus
{
    PendingPayment = 0,
    Active = 1,
    Paused = 2,
    Cancelled = 3
}

public enum BookingType
{
    VideoCall = 0,
    InPerson = 1
}

public enum BookingStatus
{
    Requested = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Converted = 2,
    Closed = 3
}

public enum PaymentType
{
    Subscription = 0,
    Booking = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Refunded = 2,
    Failed = 3
}
