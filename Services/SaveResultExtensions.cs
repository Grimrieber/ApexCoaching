using Microsoft.EntityFrameworkCore;
using OnlineTrainer.Data;

namespace OnlineTrainer.Services;

/// <summary>
/// Save helper for UI components.
/// </summary>
/// <remarks>
/// Blazor Server has no page-level error boundary by default: an exception thrown from a component
/// event handler tears down the circuit and the user sees a generic reconnect prompt, losing whatever
/// they had typed. These helpers turn a failed write into a message the page can render in place, so
/// the user can correct or retry. The underlying exception is always logged.
/// </remarks>
public static class SaveResultExtensions
{
    private const string SaveFailed =
        "We couldn't save your changes. Please try again, and contact support if it keeps happening.";

    private const string Conflict =
        "Someone else changed this while you were editing. Reload the page and try again.";

    /// <summary>
    /// Saves pending changes. Returns <c>null</c> on success, or a user-safe message on failure.
    /// </summary>
    /// <param name="db">Context to save.</param>
    /// <param name="log">Logger for the calling component.</param>
    /// <param name="operation">Short description used in the log entry, e.g. "Update trainer profile".</param>
    public static async Task<string?> TrySaveAsync(this ApplicationDbContext db, ILogger log, string operation)
    {
        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another write beat us to it. Recoverable by reloading, so say that rather than
            // presenting it as a generic failure.
            log.LogWarning(ex, "{Operation} hit a concurrency conflict", operation);
            return Conflict;
        }
        catch (DbUpdateException ex)
        {
            // Constraint violation, connection loss, or a bad value that validation missed.
            log.LogError(ex, "{Operation} failed to save", operation);
            return SaveFailed;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "{Operation} failed unexpectedly", operation);
            return SaveFailed;
        }
    }
}
