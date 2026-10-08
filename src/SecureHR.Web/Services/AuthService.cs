using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SecureHR.Contracts;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace SecureHR.Web.Services
{
    /// <summary>
    /// Login / logout for the current circuit. The token is kept in memory
    /// (<see cref="AuthTokenProvider"/>) and persisted in ProtectedLocalStorage so the user
    /// stays signed in across tabs and browser restarts until the token expires.
    /// ProtectedLocalStorage encrypts the value with ASP.NET Core Data Protection, so script
    /// running in the browser can't read a usable token.
    /// </summary>
    public class AuthService(
        HttpClient httpClient,
        JwtAuthenticationStateProvider authenticationStateProvider,
        ProtectedLocalStorage browserStorage,
        AuthTokenProvider tokenProvider,
        ILogger<AuthService> logger)
    {
        private const string TokenKey = "auth_token";

        // Single restore per circuit; Blazor Server runs a circuit's code one piece at a time
        private Task? _initialization;

        public string? Token => tokenProvider.HasToken ? tokenProvider.CurrentToken : null;
        public bool IsAuthenticated => tokenProvider.HasToken;
        public string? Username => tokenProvider.Username;
        public string? Role => tokenProvider.Role;

        public bool IsInRole(params string[] roles) =>
            IsAuthenticated && tokenProvider.Role is { } role && roles.Contains(role);

        /// <summary>
        /// Event fired when authentication state changes (login/logout)
        /// </summary>
        public event Action? OnAuthStateChanged;

        /// <summary>
        /// Restores the token from browser storage. Must be called after the first render
        /// (browser storage isn't available during prerendering). Safe to call from several
        /// components at once: the restore runs only once per circuit.
        /// </summary>
        public Task InitializeAsync() => _initialization ??= RestoreTokenAsync();

        private async Task RestoreTokenAsync()
        {
            if (IsAuthenticated)
                return;

            string? storedToken = null;
            try
            {
                var result = await browserStorage.GetAsync<string>(TokenKey);
                storedToken = result.Success ? result.Value : null;
            }
            catch (CryptographicException)
            {
                // Stored value was protected with a different key (e.g. keys rotated); discard it.
                logger.LogInformation("Discarding unreadable stored auth token");
            }

            if (storedToken is null)
                return;

            if (tokenProvider.SetToken(storedToken))
            {
                authenticationStateProvider.NotifyChanged();
            }
            else
            {
                // Expired or malformed
                await browserStorage.DeleteAsync(TokenKey);
            }
        }

        /// <summary>Signs in. Returns null on success, otherwise a message to show the user.</summary>
        public async Task<string?> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequestDto
                {
                    Username = username,
                    Password = password
                };

                var response = await httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Login failed for {Username} with status {StatusCode}", username, (int)response.StatusCode);
                    return response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                        ? "Too many sign-in attempts. Please wait a minute and try again."
                        : "Invalid username or password.";
                }

                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (loginResponse?.Token is null || !tokenProvider.SetToken(loginResponse.Token))
                {
                    logger.LogWarning("Login response for {Username} did not contain a usable token", username);
                    return "Could not sign in. Please try again.";
                }

                await browserStorage.SetAsync(TokenKey, loginResponse.Token);
                authenticationStateProvider.NotifyChanged();
                OnAuthStateChanged?.Invoke();
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login request failed");
                return "Could not reach the server. Please try again.";
            }
        }

        public async Task Logout()
        {
            // Let any in-flight restore finish first, or it could sign the user back in
            // after the token has been cleared. Afterwards, nothing is restored again.
            if (_initialization is not null)
                await _initialization;
            _initialization = Task.CompletedTask;

            tokenProvider.ClearToken();
            await browserStorage.DeleteAsync(TokenKey);

            authenticationStateProvider.NotifyChanged();
            OnAuthStateChanged?.Invoke();
        }
    }
}
