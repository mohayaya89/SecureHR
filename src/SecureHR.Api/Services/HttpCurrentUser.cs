using SecureHR.Application.Interfaces.Services;
using System.Security.Claims;

namespace SecureHR.Api.Services
{
    /// <summary>
    /// Reads the signed-in user from the current request's JWT claims.
    /// </summary>
    public class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Username => User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
    }
}
