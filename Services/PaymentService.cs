using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineTrainer.Data;
using Stripe;
using Stripe.Checkout;

namespace OnlineTrainer.Services;

public record CheckoutResult(bool Success, string RedirectUrl, string? Error = null);

/// <summary>
/// Marketplace payments via Stripe Connect. The platform takes <see cref="Trainer.CommissionRate"/>
/// as an application fee on every charge; the remainder is routed to the trainer's connected account.
/// When Stripe keys are not configured the service runs in <b>demo mode</b>: it records the payment
/// and activates the engagement/booking locally so the whole flow is demonstrable without real money.
/// </summary>
/// <remarks>
/// Every public method returns <see cref="CheckoutResult"/> rather than throwing. Callers are UI
/// components, and an unhandled exception mid-checkout would drop the user on an error page with no
/// indication of whether they had been charged. Provider detail is logged, never surfaced.
/// </remarks>
public class PaymentService
{
    private const string ProviderUnavailable =
        "We couldn't reach our payment provider. No charge was made - please try again in a moment.";

    private readonly ApplicationDbContext _db;
    private readonly StripeSettings _settings;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(ApplicationDbContext db, IOptions<StripeSettings> settings, ILogger<PaymentService> log)
    {
        _db = db;
        _settings = settings.Value;
        _log = log;
        if (_settings.IsConfigured)
            StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public bool LiveMode => _settings.IsConfigured;

    /// <summary>Platform's cut for a given gross amount and trainer commission rate.</summary>
    public static decimal PlatformFee(decimal amount, decimal commissionRate)
        => Math.Round(amount * commissionRate, 2, MidpointRounding.AwayFromZero);

    // ---- Failure handling ----------------------------------------------------------------

    /// <summary>
    /// Logs the real cause and maps it to a message that is safe to show a user. Stripe messages are
    /// only passed through for card errors, which are the ones a user can act on; anything else could
    /// disclose account or configuration detail.
    /// </summary>
    private CheckoutResult Fail(Exception ex, string operation, string fallbackUrl)
    {
        _log.LogError(ex, "{Operation} failed", operation);

        var message = ex switch
        {
            StripeException se when se.StripeError?.Type == "card_error"
                => se.StripeError.Message ?? ProviderUnavailable,
            StripeException => ProviderUnavailable,
            TaskCanceledException or TimeoutException
                => "The payment provider took too long to respond. No charge was made - please try again.",
            HttpRequestException => ProviderUnavailable,
            DbUpdateException
                => "We couldn't save your details. Please try again, and contact support if it keeps happening.",
            _ => ProviderUnavailable
        };

        return new CheckoutResult(false, fallbackUrl, message);
    }

    /// <summary>True when the trainer can actually receive a transfer.</summary>
    private static bool CanReceivePayouts(Trainer trainer)
        => !string.IsNullOrEmpty(trainer.StripeAccountId) && trainer.StripeOnboarded;

    // ---- Trainer onboarding (Stripe Connect Express) -------------------------------------

    /// <summary>
    /// Returns a URL to send the trainer to in order to connect their payout account.
    /// Demo mode marks the trainer onboarded immediately and returns a local return URL.
    /// </summary>
    public async Task<CheckoutResult> CreateOnboardingLinkAsync(Trainer trainer, string returnUrl, string refreshUrl)
    {
        try
        {
            if (!LiveMode)
            {
                trainer.StripeAccountId ??= $"acct_demo_{trainer.Id}";
                trainer.StripeOnboarded = true;
                await _db.SaveChangesAsync();
                return new CheckoutResult(true, returnUrl + "?demo=1");
            }

            if (string.IsNullOrEmpty(trainer.StripeAccountId))
            {
                var account = await new AccountService().CreateAsync(new AccountCreateOptions
                {
                    Type = "express",
                    Email = trainer.User?.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                    }
                });
                trainer.StripeAccountId = account.Id;

                // Persist before requesting the link. If the save fails we must not hand back a link
                // for an account id we never recorded, or the next attempt creates a second account.
                await _db.SaveChangesAsync();
            }

            var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
            {
                Account = trainer.StripeAccountId,
                ReturnUrl = returnUrl,
                RefreshUrl = refreshUrl,
                Type = "account_onboarding"
            });
            return new CheckoutResult(true, link.Url);
        }
        catch (Exception ex)
        {
            return Fail(ex, "Stripe Connect onboarding", returnUrl);
        }
    }

    // ---- Subscriptions (monthly coaching packages) ---------------------------------------

    public async Task<CheckoutResult> StartSubscriptionAsync(
        ClientProfile client, ServicePackage package, Trainer trainer, string baseUrl)
    {
        var fee = PlatformFee(package.PriceMonthly, trainer.CommissionRate);

        try
        {
            if (!LiveMode)
            {
                var engagement = new Engagement
                {
                    ClientProfileId = client.Id,
                    TrainerId = trainer.Id,
                    ServicePackageId = package.Id,
                    MonthlyPrice = package.PriceMonthly,
                    Status = EngagementStatus.Active,
                    StripeSubscriptionId = $"sub_demo_{Guid.NewGuid():N}"
                };
                _db.Engagements.Add(engagement);
                client.TrainerId = trainer.Id;
                RecordPayment(client.Id, trainer.Id, PaymentType.Subscription, package.PriceMonthly, fee, "demo");
                await _db.SaveChangesAsync();
                return new CheckoutResult(true, $"{baseUrl}/client/billing?welcome=1");
            }

            // Stripe rejects a transfer to an account that has not completed onboarding, and the
            // error it returns is opaque. Check first so the client gets a message that explains itself.
            if (!CanReceivePayouts(trainer))
            {
                _log.LogWarning("Subscription blocked: trainer {TrainerId} has not completed payout onboarding", trainer.Id);
                return new CheckoutResult(false, baseUrl,
                    "This trainer hasn't finished setting up payments yet. Please try again later.");
            }

            var session = await new SessionService().CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = $"{baseUrl}/client/billing?welcome=1",
                CancelUrl = $"{baseUrl}/trainers/{trainer.Slug}",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(package.PriceMonthly * 100),
                            Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = "month" },
                            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = package.Name }
                        }
                    }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    ApplicationFeePercent = trainer.CommissionRate * 100,
                    TransferData = new SessionSubscriptionDataTransferDataOptions
                    {
                        Destination = trainer.StripeAccountId
                    }
                }
            });
            return new CheckoutResult(true, session.Url);
        }
        catch (Exception ex)
        {
            return Fail(ex, "Stripe subscription checkout", baseUrl);
        }
    }

    // ---- One-off session bookings --------------------------------------------------------

    public async Task<CheckoutResult> StartBookingAsync(Booking booking, Trainer trainer, string baseUrl)
    {
        var fee = PlatformFee(booking.Price, trainer.CommissionRate);

        try
        {
            if (!LiveMode)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.StripePaymentIntentId = $"pi_demo_{Guid.NewGuid():N}";
                RecordPayment(booking.ClientProfileId, trainer.Id, PaymentType.Booking, booking.Price, fee, "demo");
                await _db.SaveChangesAsync();
                return new CheckoutResult(true, $"{baseUrl}/client/bookings?booked=1");
            }

            if (!CanReceivePayouts(trainer))
            {
                _log.LogWarning("Booking blocked: trainer {TrainerId} has not completed payout onboarding", trainer.Id);
                return new CheckoutResult(false, $"{baseUrl}/client/book",
                    "This trainer hasn't finished setting up payments yet. Please try again later.");
            }

            var session = await new SessionService().CreateAsync(new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{baseUrl}/client/bookings?booked=1",
                CancelUrl = $"{baseUrl}/client/book",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(booking.Price * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{booking.DurationMinutes}-min session with {trainer.User?.FullName}"
                            }
                        }
                    }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = (long)(fee * 100),
                    TransferData = new SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = trainer.StripeAccountId
                    }
                }
            });
            return new CheckoutResult(true, session.Url);
        }
        catch (Exception ex)
        {
            return Fail(ex, "Stripe booking checkout", $"{baseUrl}/client/book");
        }
    }

    private void RecordPayment(int clientId, int trainerId, PaymentType type,
        decimal amount, decimal fee, string reference)
    {
        _db.Payments.Add(new Payment
        {
            ClientProfileId = clientId,
            TrainerId = trainerId,
            Type = type,
            Status = PaymentStatus.Succeeded,
            Amount = amount,
            PlatformFee = fee,
            TrainerNet = amount - fee,
            StripeReference = reference
        });
    }
}
