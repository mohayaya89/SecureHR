using SecureHR.Web.Client;
using SecureHR.Web.Components;
using SecureHR.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Auth state is per circuit (per user session). These must all be Scoped:
// a Singleton token store would share one user's token with every user.
builder.Services.AddScoped<AuthTokenProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// AuthService is Scoped (not a typed HttpClient, which would be transient) so every
// component in the circuit shares it and its OnAuthStateChanged event.
builder.Services.AddScoped(sp => ActivatorUtilities.CreateInstance<AuthService>(
    sp, sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthClient")));

// Configure named HttpClient for AuthService
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});

// Typed API clients; each attaches the signed-in user's token (see ApiClientBase)
void AddApiClient<TClient>() where TClient : class =>
    builder.Services.AddHttpClient<TClient>(client => client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!));

AddApiClient<EmployeeApiClient>();
AddApiClient<DepartmentApiClient>();
AddApiClient<PayrollApiClient>();
AddApiClient<ReportApiClient>();
AddApiClient<AuditLogApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
