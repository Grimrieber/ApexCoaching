using System.ComponentModel.DataAnnotations;

namespace OnlineTrainer.Data;

/// <summary>A coaching program a trainer assigns to a client (the delivered content).</summary>
public class TrainingProgram
{
    public int Id { get; set; }

    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<Workout> Workouts { get; set; } = new List<Workout>();
}

/// <summary>A single training day within a program.</summary>
public class Workout
{
    public int Id { get; set; }

    public int TrainingProgramId { get; set; }
    public TrainingProgram? TrainingProgram { get; set; }

    [MaxLength(40)]
    public string DayLabel { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public ICollection<ExerciseItem> Exercises { get; set; } = new List<ExerciseItem>();
}

/// <summary>A prescribed exercise within a workout.</summary>
public class ExerciseItem
{
    public int Id { get; set; }

    public int WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Sets { get; set; }

    [MaxLength(20)]
    public string Reps { get; set; } = string.Empty;

    public int RestSeconds { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int OrderIndex { get; set; }
}
