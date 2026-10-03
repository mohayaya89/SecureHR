using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SecureHR.Api.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Services;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IAuthenticationService authenticationService) : ControllerBase
    {
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto loginRequest, 
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(loginRequest.Username) || 
                string.IsNullOrWhiteSpace(loginRequest.Password))
                return BadRequest("Username and password are required");

            var result = await authenticationService.AuthenticateAsync(
                loginRequest.Username, 
                loginRequest.Password, 
                ct);

            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            var response = new LoginResponseDto
            {
                Token = result.Token ?? string.Empty,
                Username = result.Username ?? string.Empty,
                Role = result.Role ?? string.Empty,
                ExpiresAt = result.ExpiresAt ?? DateTime.UtcNow.AddHours(24)
            };

            return Ok(response);
        }
    }
}
