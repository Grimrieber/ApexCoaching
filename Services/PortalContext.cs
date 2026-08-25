using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using OnlineTrainer.Data;

namespace OnlineTrainer.Services;

/// <summary>Resolves the signed-in user's trainer/client profile for portal pages.</summary>
public class PortalContext(ApplicationDbContext db, AuthenticationStateProvider auth)
{
    public async Task<string?> GetUserIdAsync()
    {
        var state = await auth.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<Trainer?> GetTrainerAsync()
    {
        var uid = await GetUserIdAsync();
        if (uid is null) return null;
        return await db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.UserId == uid);
    }

    public async Task<ClientProfile?> GetClientAsync()
    {
        var uid = await GetUserIdAsync();
        if (uid is null) return null;
        return await db.ClientProfiles
            .Include(c => c.User)
            .Include(c => c.Trainer).ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(c => c.UserId == uid);
    }
}
