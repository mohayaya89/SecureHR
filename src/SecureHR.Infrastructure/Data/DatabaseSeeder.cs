using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Enums;
using SecureHR.Infrastructure.Cryptography;

namespace SecureHR.Infrastructure.Data;

public static class DatabaseSeeder
{
    /// <param name="demoUserPassword">
    /// When set (Development only), also creates the "hrstaff" (HR) and "viewer" (ReadOnly)
    /// demo users so role-based access can be tried out.
    /// </param>
    /// <param name="includeDemoData">
    /// Development only: when the database has no employees, adds the demo employees in
    /// <see cref="DemoData"/>.
    /// </param>
    public static async Task SeedAsync(
        SecureHRDbContext dbContext,
        string? adminPassword,
        string? demoUserPassword = null,
        bool includeDemoData = false)
    {
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Users.AnyAsync())
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "No users exist and 'Seed:AdminPassword' is not configured. " +
                    "Set it (e.g. environment variable Seed__AdminPassword) to create the initial admin user.");
            }

            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = PasswordHashing.Hash(adminPassword),
                Role = UserRole.Admin
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(demoUserPassword))
        {
            await EnsureUserAsync(dbContext, "hrstaff", demoUserPassword, UserRole.HR);
            await EnsureUserAsync(dbContext, "viewer", demoUserPassword, UserRole.ReadOnly);
        }

        if (!await dbContext.Departments.AnyAsync())
        {
            dbContext.Departments.AddRange(DemoData.Departments.Select(d => new Department
            {
                Name = d.Name
            }));

            await dbContext.SaveChangesAsync();
        }

        if (includeDemoData && !await dbContext.Employees.AnyAsync())
        {
            await SeedDemoEmployeesAsync(dbContext);
        }
    }

    // Development demo data: see DemoData. Only runs on a database with no employees.
    private static async Task SeedDemoEmployeesAsync(SecureHRDbContext dbContext)
    {
        var departments = await dbContext.Departments.ToDictionaryAsync(d => d.Name);

        dbContext.Employees.AddRange(DemoData.Employees.Select(e => new Employee
        {
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            JobTitle = e.JobTitle,
            DepartmentId = departments[e.Department].Id,
            HireDate = e.HireDate,
            AnnualSalary = e.AnnualSalary,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }));

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(SecureHRDbContext dbContext, string username, string password, UserRole role)
    {
        if (await dbContext.Users.AnyAsync(u => u.Username == username))
            return;

        dbContext.Users.Add(new User
        {
            Username = username,
            PasswordHash = PasswordHashing.Hash(password),
            Role = role
        });
        await dbContext.SaveChangesAsync();
    }
}
