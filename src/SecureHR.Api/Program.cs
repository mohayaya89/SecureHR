using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureHR.Api.Authorization;
using SecureHR.Api.Middleware;
using SecureHR.Api.Services;
using SecureHR.Application;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// The signed-in user, used by the audit trail
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "'Jwt:Secret' must be configured and at least 32 characters. " +
        "Development uses appsettings.Development.json; other environments must set it " +
        "(e.g. environment variable Jwt__Secret).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Role-based policies; every endpoint requires login unless marked [AllowAnonymous]
builder.Services.AddAuthorization(AuthPolicies.Configure);
builder.Services.AddRateLimiter(options =>
    RateLimitPolicies.Configure(options, builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5)));

var app = builder.Build();

// Unhandled exceptions and bare error status codes become ProblemDetails responses
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Development also gets demo users and demo employees (see DemoData)
await app.Services.InitializeDatabaseAsync(
    app.Configuration["Seed:AdminPassword"],
    app.Environment.IsDevelopment()
    ? app.Configuration["Seed:DemoUserPassword"]
    : null,
    includeDemoData: app.Environment.IsDevelopment());

app.Run();

// Exposed for integration tests (WebApplicationFactory<Program>)
public partial class Program;
