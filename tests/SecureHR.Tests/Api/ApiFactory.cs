using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using SecureHR.Contracts;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SecureHR.Tests.Api
{
    /// <summary>
    /// Runs the real API in memory against a temporary SQLite file. Uses the Development
    /// environment so the dev keys and demo users (admin, hrstaff, viewer) are available.
    /// </summary>
    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public const string AdminPassword = "AdminPassword123!";
        public const string DemoPassword = "DemoPassword123!";

        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"securehr-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
            builder.UseSetting("RateLimiting:LoginPermitLimit", "1000");
        }

        /// <summary>An HttpClient signed in as the given user.</summary>
        public async Task<HttpClient> CreateClientAsync(string username, string password)
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDto { Username = username, Password = password });
            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
            return client;
        }

        public Task<HttpClient> AdminAsync() => CreateClientAsync("admin", AdminPassword);
        public Task<HttpClient> HrAsync() => CreateClientAsync("hrstaff", DemoPassword);
        public Task<HttpClient> ViewerAsync() => CreateClientAsync("viewer", DemoPassword);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }
}
