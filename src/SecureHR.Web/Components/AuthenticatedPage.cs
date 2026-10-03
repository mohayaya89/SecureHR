using Microsoft.AspNetCore.Components;
using SecureHR.Web.Services;

namespace SecureHR.Web.Components
{
    /// <summary>
    /// Base class for pages that require a signed-in user. After the first render (browser
    /// storage isn't available during prerendering) it restores the session and either
    /// redirects to /login or calls <see cref="OnAuthenticatedAsync"/>.
    ///
    /// This only controls what the UI shows; the API enforces authentication and roles.
    /// </summary>
    public abstract class AuthenticatedPage : ComponentBase
    {
        [Inject] protected AuthService AuthService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        /// <summary>True once the session is restored and the user is signed in.</summary>
        protected bool IsReady { get; private set; }

        /// <summary>Load the page's data here. Runs once, after sign-in is confirmed.</summary>
        protected virtual Task OnAuthenticatedAsync() => Task.CompletedTask;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            await AuthService.InitializeAsync();
            if (!AuthService.IsAuthenticated)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            IsReady = true;
            await OnAuthenticatedAsync();
            StateHasChanged();
        }
    }
}
