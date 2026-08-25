namespace OnlineTrainer.Services;

/// <summary>
/// Stripe configuration bound from the "Stripe" section of appsettings / user-secrets.
/// Leave keys empty to run in demo mode (no real charges; flows are simulated locally).
/// </summary>
public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>True once a real Stripe secret key has been provided.</summary>
    public bool IsConfigured => SecretKey.StartsWith("sk_", StringComparison.Ordinal);
}
