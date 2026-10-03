using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SecureHR.Web.Services
{
    /// <summary>
    /// Exposes the current circuit's token as a ClaimsPrincipal (name + role) to Blazor.
    /// </summary>
    public class JwtAuthenticationStateProvider(AuthTokenProvider tokenProvider) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!tokenProvider.HasToken)
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            var claims = new List<Claim>();
            if (tokenProvider.Username is { } name)
                claims.Add(new Claim(ClaimTypes.Name, name));
            if (tokenProvider.Role is { } role)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, "jwt");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        public bool IsAuthenticated => tokenProvider.HasToken;

        /// <summary>
        /// Call after the token in <see cref="AuthTokenProvider"/> changes (login, logout, restore).
        /// </summary>
        public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
