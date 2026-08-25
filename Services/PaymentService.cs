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
public class PaymentService
{
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

    // ---- Trainer onboarding (Stripe Connect Express) -------------------------------------

    /// <summary>
    /// Returns a URL to send the trainer to in order to connect their payout account.
    /// Demo mode marks the trainer onboarded immediately and returns a local return URL.
    /// </summary>
    public async Task<string> CreateOnboardingLinkAsync(Trainer trainer, string returnUrl, string refreshUrl)
    {
        if (!LiveMode)
        {
            trainer.StripeAccountId ??= $"acct_demo_{trainer.Id}";
            trainer.StripeOnboarded = true;
            await _db.SaveChangesAsync();
            return returnUrl + "?demo=1";
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
            await _db.SaveChangesAsync();
        }

        var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
        {
            Account = trainer.StripeAccountId,
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl,
            Type = "account_onboarding"
        });
        return link.Url;
    }

    // ---- Subscriptions (monthly coaching packages) ---------------------------------------

    public async Task<CheckoutResult> StartSubscriptionAsync(
        ClientProfile client, ServicePackage package, Trainer trainer, string baseUrl)
    {
        var fee = PlatformFee(package.PriceMonthly, trainer.CommissionRate);

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

        try
        {
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
        catch (StripeException ex)
        {
            _log.LogError(ex, "Stripe subscription checkout failed");
            return new CheckoutResult(false, baseUrl, ex.Message);
        }
    }

    // ---- One-off session bookings --------------------------------------------------------

    public async Task<CheckoutResult> StartBookingAsync(Booking booking, Trainer trainer, string baseUrl)
    {
        var fee = PlatformFee(booking.Price, trainer.CommissionRate);

        if (!LiveMode)
        {
            booking.Status = BookingStatus.Confirmed;
            booking.StripePaymentIntentId = $"pi_demo_{Guid.NewGuid():N}";
            RecordPayment(booking.ClientProfileId, trainer.Id, PaymentType.Booking, booking.Price, fee, "demo");
            await _db.SaveChangesAsync();
            return new CheckoutResult(true, $"{baseUrl}/client/bookings?booked=1");
        }

        try
        {
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
        catch (StripeException ex)
        {
            _log.LogError(ex, "Stripe booking checkout failed");
            return new CheckoutResult(false, baseUrl, ex.Message);
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
