using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace SecureHR.Api.Authorization
{
    public static class RateLimitPolicies
    {
        public const string Login = nameof(Login);

        /// <summary>
        /// Limits login attempts per client IP to slow down password guessing.
        /// </summary>
        /// <param name="loginPermitLimit">Attempts allowed per IP per minute (config: RateLimiting:LoginPermitLimit).</param>
        public static void Configure(RateLimiterOptions options, int loginPermitLimit)
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        }
    }
}
