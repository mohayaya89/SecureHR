using System.Text;
using System.Text.Json;

namespace SecureHR.Web.Services
{
    /// <summary>
    /// Holds the signed-in user's JWT for one Blazor circuit (one browser tab / user session).
    /// Registered as Scoped: a Singleton here would share one user's token with every user.
    ///
    /// Claims are read from the token payload for display and UI decisions only; the API
    /// validates the signature and enforces authorization on every request.
    /// </summary>
    public class AuthTokenProvider
    {
        private const string NameClaimUri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string RoleClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        public string? CurrentToken { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }

        public bool HasToken =>
            CurrentToken is not null && ExpiresAt is { } expires && expires > DateTimeOffset.UtcNow;

        /// <summary>
        /// Stores the token if it is well-formed and not expired.
        /// </summary>
        /// <returns>False (and clears state) when the token is unusable.</returns>
        public bool SetToken(string? token)
        {
            ClearToken();

            if (string.IsNullOrWhiteSpace(token) || !TryReadPayload(token, out var payload))
                return false;

            using (payload)
            {
                var root = payload.RootElement;
                if (!root.TryGetProperty("exp", out var exp) || !exp.TryGetInt64(out var expSeconds))
                    return false;

                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                if (expiresAt <= DateTimeOffset.UtcNow)
                    return false;

                CurrentToken = token;
                ExpiresAt = expiresAt;
                Username = ReadString(root, NameClaimUri, "unique_name", "name");
                Role = ReadString(root, RoleClaimUri, "role");
                return true;
            }
        }

        public void ClearToken()
        {
            CurrentToken = null;
            Username = null;
            Role = null;
            ExpiresAt = null;
        }

        private static bool TryReadPayload(string token, out JsonDocument payload)
        {
            payload = null!;
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            try
            {
                var base64 = parts[1].Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
                payload = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
                return true;
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                return false;
            }
        }

        private static string? ReadString(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString();
            }
            return null;
        }
    }
}
