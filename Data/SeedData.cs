using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace OnlineTrainer.Data;

/// <summary>
/// Applies migrations and seeds demo accounts and content so the concept is fully explorable.
/// Idempotent: re-running does nothing once a trainer exists.
/// </summary>
public static class SeedData
{
    public const string DemoPassword = "Demo123!";
    public const string AdminEmail = "admin@apexcoaching.test";
    public const string CoachEmail = "coach@apexcoaching.test";
    public const string ClientEmail = "client@apexcoaching.test";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (await db.Trainers.AnyAsync())
            return; // already seeded

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ---- Admin ----
        await CreateUserAsync(users, AdminEmail, "Site Admin", Roles.Admin);

        // ---- Trainer 1: Alex Rivera (fully built out, has a client) ----
        var alexUser = await CreateUserAsync(users, CoachEmail, "Alex Rivera", Roles.Trainer);
        var alex = new Trainer
        {
            UserId = alexUser.Id,
            Slug = "alex-rivera",
            Headline = "Strength & fat-loss coach for busy professionals",
            Bio = "NASM-certified coach with 9 years helping desk-bound professionals get strong, " +
                  "lean, and pain-free. My approach is simple: sustainable nutrition, smart progressive " +
                  "overload, and real accountability — no fads, no guesswork.",
            Specialties = "Strength, Fat loss, Mobility, Habit coaching",
            Location = "Austin, TX (online worldwide)",
            YearsExperience = 9,
            HourlyRate = 80m,
            CommissionRate = 0.20m,
            IsApproved = true,
            IsAcceptingClients = true,
            StripeOnboarded = false
        };
        alex.Packages = BuildPackages();
        db.Trainers.Add(alex);

        // ---- Trainer 2: Jordan Lee (marketplace breadth) ----
        var jordanUser = await CreateUserAsync(users, "jordan@apexcoaching.test", "Jordan Lee", Roles.Trainer);
        var jordan = new Trainer
        {
            UserId = jordanUser.Id,
            Slug = "jordan-lee",
            Headline = "Postpartum & women's strength specialist",
            Bio = "Pre/postnatal certified trainer focused on rebuilding strength safely and confidently. " +
                  "Programming that respects your recovery and fits real-mom schedules.",
            Specialties = "Postpartum, Women's strength, Core, Conditioning",
            Location = "Remote (US)",
            YearsExperience = 6,
            HourlyRate = 70m,
            CommissionRate = 0.20m,
            IsApproved = true,
            IsAcceptingClients = true
        };
        jordan.Packages = BuildPackages();
        db.Trainers.Add(jordan);

        await db.SaveChangesAsync();

        // ---- Demo client engaged with Alex ----
        var clientUser = await CreateUserAsync(users, ClientEmail, "Sam Carter", Roles.Client);
        var client = new ClientProfile
        {
            UserId = clientUser.Id,
            TrainerId = alex.Id,
            Goal = "Lose 15 lbs and get stronger without living in the gym",
            ExperienceLevel = "Intermediate"
        };
        db.ClientProfiles.Add(client);
        await db.SaveChangesAsync();

        var coachedPackage = alex.Packages.First(p => p.Tier == PackageTier.Coached);
        db.Engagements.Add(new Engagement
        {
            ClientProfileId = client.Id,
            TrainerId = alex.Id,
            ServicePackageId = coachedPackage.Id,
            MonthlyPrice = coachedPackage.PriceMonthly,
            Status = EngagementStatus.Active,
            StripeSubscriptionId = "sub_demo_seed"
        });

        db.Payments.Add(new Payment
        {
            ClientProfileId = client.Id,
            TrainerId = alex.Id,
            Type = PaymentType.Subscription,
            Status = PaymentStatus.Succeeded,
            Amount = coachedPackage.PriceMonthly,
            PlatformFee = PaymentFee(coachedPackage.PriceMonthly, alex.CommissionRate),
            TrainerNet = coachedPackage.PriceMonthly - PaymentFee(coachedPackage.PriceMonthly, alex.CommissionRate),
            StripeReference = "demo_seed"
        });

        // ---- Assigned program with content ----
        db.TrainingPrograms.Add(BuildProgram(alex.Id, client.Id));

        // ---- An upcoming booked session ----
        db.Bookings.Add(new Booking
        {
            ClientProfileId = client.Id,
            TrainerId = alex.Id,
            ScheduledAt = DateTime.UtcNow.Date.AddDays(3).AddHours(17),
            DurationMinutes = 45,
            Type = BookingType.VideoCall,
            Status = BookingStatus.Confirmed,
            Price = 60m,
            Notes = "Form check: squat & deadlift"
        });

        // ---- Inbound leads ----
        db.Leads.Add(new Lead { Name = "Taylor Brooks", Email = "taylor@example.com", Goal = "Build muscle for summer", TrainerId = alex.Id });
        db.Leads.Add(new Lead { Name = "Morgan Diaz", Email = "morgan@example.com", Goal = "Get back in shape postpartum", TrainerId = jordan.Id });

        await db.SaveChangesAsync();
    }

    private static decimal PaymentFee(decimal amount, decimal rate)
        => Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> users, string email, string fullName, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };
        await users.CreateAsync(user, DemoPassword);
        await users.AddToRoleAsync(user, role);
        return user;
    }

    private static List<ServicePackage> BuildPackages() => new()
    {
        new ServicePackage
        {
            Name = "Starter", Tier = PackageTier.Starter, PriceMonthly = 99m,
            Description = "Custom training program with monthly accountability.",
            Features = "Custom training program\nMonthly check-in\nApp & messaging support"
        },
        new ServicePackage
        {
            Name = "Coached", Tier = PackageTier.Coached, PriceMonthly = 199m, IsPopular = true,
            Description = "Training and nutrition with weekly coaching.",
            Features = "Training + nutrition programming\nWeekly check-ins & adjustments\nVideo form reviews\nPriority messaging"
        },
        new ServicePackage
        {
            Name = "1:1 Premium", Tier = PackageTier.Premium, PriceMonthly = 349m,
            Description = "High-touch coaching with live video sessions.",
            Features = "Everything in Coached\nLive video sessions\nDaily accountability\nFully bespoke programming"
        }
    };

    private static TrainingProgram BuildProgram(int trainerId, int clientId) => new()
    {
        TrainerId = trainerId,
        ClientProfileId = clientId,
        Name = "12-Week Strength & Lean",
        Description = "Upper/lower split progressing in waves, paired with a moderate calorie deficit.",
        StartDate = DateTime.UtcNow.Date.AddDays(-9),
        Workouts = new List<Workout>
        {
            new()
            {
                DayLabel = "Day 1", Name = "Push", OrderIndex = 0,
                Exercises = new List<ExerciseItem>
                {
                    new() { Name = "Barbell Bench Press", Sets = 4, Reps = "6-8", RestSeconds = 120, OrderIndex = 0 },
                    new() { Name = "Incline Dumbbell Press", Sets = 3, Reps = "8-10", RestSeconds = 90, OrderIndex = 1 },
                    new() { Name = "Overhead Press", Sets = 3, Reps = "8-10", RestSeconds = 90, OrderIndex = 2 },
                    new() { Name = "Cable Fly", Sets = 3, Reps = "12-15", RestSeconds = 60, OrderIndex = 3 },
                    new() { Name = "Triceps Pushdown", Sets = 3, Reps = "12-15", RestSeconds = 60, OrderIndex = 4 },
                }
            },
            new()
            {
                DayLabel = "Day 2", Name = "Pull", OrderIndex = 1,
                Exercises = new List<ExerciseItem>
                {
                    new() { Name = "Deadlift", Sets = 3, Reps = "5", RestSeconds = 150, OrderIndex = 0 },
                    new() { Name = "Pull-Up", Sets = 4, Reps = "AMRAP", RestSeconds = 90, OrderIndex = 1 },
                    new() { Name = "Barbell Row", Sets = 3, Reps = "8-10", RestSeconds = 90, OrderIndex = 2 },
                    new() { Name = "Face Pull", Sets = 3, Reps = "15", RestSeconds = 60, OrderIndex = 3 },
                    new() { Name = "Dumbbell Curl", Sets = 3, Reps = "10-12", RestSeconds = 60, OrderIndex = 4 },
                }
            },
            new()
            {
                DayLabel = "Day 3", Name = "Legs", OrderIndex = 2,
                Exercises = new List<ExerciseItem>
                {
                    new() { Name = "Back Squat", Sets = 4, Reps = "6-8", RestSeconds = 150, OrderIndex = 0 },
                    new() { Name = "Romanian Deadlift", Sets = 3, Reps = "8-10", RestSeconds = 90, OrderIndex = 1 },
                    new() { Name = "Leg Press", Sets = 3, Reps = "10-12", RestSeconds = 90, OrderIndex = 2 },
                    new() { Name = "Walking Lunge", Sets = 3, Reps = "12/leg", RestSeconds = 60, OrderIndex = 3 },
                    new() { Name = "Calf Raise", Sets = 4, Reps = "15", RestSeconds = 45, OrderIndex = 4 },
                }
            }
        }
    };
}
