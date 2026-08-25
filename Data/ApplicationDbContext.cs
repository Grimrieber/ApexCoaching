using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OnlineTrainer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<Engagement> Engagements => Set<Engagement>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<ExerciseItem> ExerciseItems => Set<ExerciseItem>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // One-to-one: user <-> trainer / client profile
        b.Entity<Trainer>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => t.UserId).IsUnique();
            e.HasOne(t => t.User).WithOne(u => u.TrainerProfile)
                .HasForeignKey<Trainer>(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(t => t.HourlyRate).HasPrecision(10, 2);
            e.Property(t => t.CommissionRate).HasPrecision(5, 4);
        });

        b.Entity<ClientProfile>(e =>
        {
            e.HasIndex(c => c.UserId).IsUnique();
            e.HasOne(c => c.User).WithOne(u => u.ClientProfile)
                .HasForeignKey<ClientProfile>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            // NoAction (not SetNull) to avoid multiple cascade paths into ClientProfiles
            // (User->ClientProfile cascade already exists). App code clears TrainerId when needed.
            e.HasOne(c => c.Trainer).WithMany(t => t.Clients)
                .HasForeignKey(c => c.TrainerId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ServicePackage>(e =>
        {
            e.Property(p => p.PriceMonthly).HasPrecision(10, 2);
            e.HasOne(p => p.Trainer).WithMany(t => t.Packages)
                .HasForeignKey(p => p.TrainerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Engagement>(e =>
        {
            e.Property(x => x.MonthlyPrice).HasPrecision(10, 2);
            e.HasOne(x => x.ClientProfile).WithMany(c => c.Engagements)
                .HasForeignKey(x => x.ClientProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Trainer).WithMany()
                .HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ServicePackage).WithMany()
                .HasForeignKey(x => x.ServicePackageId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Booking>(e =>
        {
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne(x => x.ClientProfile).WithMany(c => c.Bookings)
                .HasForeignKey(x => x.ClientProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Trainer).WithMany(t => t.Bookings)
                .HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TrainingProgram>(e =>
        {
            e.HasOne(x => x.ClientProfile).WithMany(c => c.Programs)
                .HasForeignKey(x => x.ClientProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Trainer).WithMany()
                .HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Workout>()
            .HasOne(w => w.TrainingProgram).WithMany(p => p.Workouts)
            .HasForeignKey(w => w.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ExerciseItem>()
            .HasOne(x => x.Workout).WithMany(w => w.Exercises)
            .HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.PlatformFee).HasPrecision(10, 2);
            e.Property(x => x.TrainerNet).HasPrecision(10, 2);
            e.HasOne(x => x.Trainer).WithMany()
                .HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ClientProfile).WithMany()
                .HasForeignKey(x => x.ClientProfileId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Lead>()
            .HasOne(l => l.Trainer).WithMany()
            .HasForeignKey(l => l.TrainerId).OnDelete(DeleteBehavior.SetNull);
    }
}
