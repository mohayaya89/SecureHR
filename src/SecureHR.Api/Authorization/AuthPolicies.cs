using Microsoft.AspNetCore.Authorization;
using SecureHR.Domain.Enums;

namespace SecureHR.Api.Authorization
{
    /// <summary>
    /// Authorization policy names. Every endpoint requires an authenticated user by
    /// default (fallback policy); these narrow access further by role.
    ///
    /// Role matrix:
    ///   ReadOnly - view employees (without salary), departments, headcount
    ///   HR       - everything ReadOnly can do, plus manage employees/departments,
    ///              payroll and salary reports
    ///   Admin    - everything, including the audit log
    /// </summary>
    public static class AuthPolicies
    {
        public const string HrStaff = nameof(HrStaff);
        public const string AdminOnly = nameof(AdminOnly);

        public static void Configure(AuthorizationOptions options)
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(HrStaff, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.HR)));
            options.AddPolicy(AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)));
        }
    }
}
