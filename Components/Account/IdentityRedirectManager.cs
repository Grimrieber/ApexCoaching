using Microsoft.AspNetCore.Components;

namespace OnlineTrainer.Components.Account;

/// <summary>
/// Helper for redirecting after a static-SSR form post. With
/// BlazorDisableThrowNavigationException=true, NavigateTo performs the redirect inline.
/// </summary>
public sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public void RedirectTo(string? uri)
    {
        uri ??= string.Empty;
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            uri = navigationManager.ToBaseRelativePath(uri);
        navigationManager.NavigateTo(uri);
    }

}
