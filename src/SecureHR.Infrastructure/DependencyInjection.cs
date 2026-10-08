using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Interfaces.Repositories;
using SecureHR.Infrastructure.Data;
using SecureHR.Infrastructure.Repository;
using SecureHR.Infrastructure.Services;

namespace SecureHR.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers persistence (SQLite + audit interceptor), repositories,
        /// and authentication. The host must also register
        /// <see cref="ICurrentUser"/>.
        /// </summary>
        /// <param name="contentRootPath">Relative SQLite paths are resolved against this folder.</param>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            string contentRootPath)
        {
            // Resolve a relative SQLite path against the project folder so the database
            // location doesn't depend on the directory the app was launched from.
            var sqlite = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Default"));
            if (!Path.IsPathRooted(sqlite.DataSource) && sqlite.DataSource != ":memory:")
            {
                sqlite.DataSource = Path.Combine(contentRootPath, sqlite.DataSource);
            }

            // Audit trail: every save of an audited entity also writes AuditLog rows (same transaction)
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddDbContext<SecureHRDbContext>((sp, opt) =>
                opt.UseSqlite(sqlite.ConnectionString)
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IPayrollRecordRepository, PayrollRecordRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IAuthenticationService, AuthenticationService>();

            return services;
        }

        /// <summary>
        /// Applies pending migrations and seeds required data.
        /// </summary>
        /// <param name="demoUserPassword">When set, also creates the demo users (Development only).</param>
        /// <param name="includeDemoData">Adds the demo employees to an empty database (Development only).</param>
        public static async Task InitializeDatabaseAsync(
            this IServiceProvider services,
            string? adminPassword,
            string? demoUserPassword,
            bool includeDemoData)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SecureHRDbContext>();

            await DatabaseSeeder.SeedAsync(dbContext, adminPassword, demoUserPassword, includeDemoData);
        }
    }
}
