# Apex Coaching — Online Personal Training Marketplace

A concept-stage web app for running an online personal-training business: a public marketing
site, a **trainer marketplace**, client & trainer portals, and a **Stripe Connect** payment layer
where the platform takes a commission (your cut) on every charge.

> **Portfolio project.** Built to exercise the current .NET stack end to end — marketplace
> domain modelling, role-scoped portals, and split-payment mechanics — rather than to ship commercially.

## Stack
- **ASP.NET Core / Blazor Web App** (.NET 10), interactive server rendering
- **SQL Server** (LocalDB by default) via **EF Core 10** (code-first + migrations)
- **ASP.NET Core Identity** — roles: `Client`, `Trainer`, `Admin`
- **Stripe.net** — Stripe Connect (Express) marketplace payments

## Run it
```bash
cd "D:\VB Projects\Web App\OnlineTrainer"
dotnet run
```
On first run it creates `OnlineTrainerDb` in LocalDB, applies migrations, and seeds demo data.
Then open the URL shown (default **http://localhost:5215**).

### Demo logins (password `Demo123!`)
| Role | Email |
|------|-------|
| Client | `client@apexcoaching.test` |
| Trainer | `coach@apexcoaching.test` |
| Admin | `admin@apexcoaching.test` |

## What works today
- **Public**: Home, How it works, Pricing, Contact (saves real leads), interactive demo dashboard
- **Marketplace**: `/trainers` directory + `/trainers/{slug}` profiles with packages
- **Auth**: register as client *or* trainer (provisions the right profile), login/logout
- **Client portal**: dashboard, assigned program (with check-offs), bookings, book-a-session, billing
- **Trainer portal**: dashboard, clients + leads, package CRUD, earnings, profile & payout setup
- **Admin**: platform overview, approve/list trainers, manage leads
- **Payments**: subscriptions + one-off bookings, with the platform fee split out per `Trainer.CommissionRate` (default 20%)

## Payments: demo vs live
With no Stripe keys set, the app runs in **demo mode** — checkout is simulated locally (no real
charge) so every flow is demonstrable. To go live, set real keys (use user-secrets, never commit):
```bash
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```
Once a `sk_...` key is present the service switches to real Stripe Connect checkout and trainer
onboarding automatically.

## Switching databases
Edit `ConnectionStrings:DefaultConnection` in `appsettings.json` to point at SQL Server DEVTEST,
Azure SQL, etc. — the schema is created from EF migrations.

## Notable next steps
- Persist program check-offs per day (currently session-local)
- Real Stripe webhook handling to confirm async payments and subscription lifecycle
- Trainer availability/calendar for bookings
- Email notifications (lead replies, booking confirmations)
- Profile photos / image uploads
