using Microsoft.EntityFrameworkCore;
using SecureHR.Infrastructure.Data;

namespace SecureHR.Tests.Infrastructure
{
    public class DatabaseSeederTests : IDisposable
    {
        private readonly TestDb _db = new();

        [Fact]
        public async Task Demo_data_seeds_all_demo_employees_into_their_departments()
        {
            await using (var context = _db.CreateContext())
                await DatabaseSeeder.SeedAsync(context, "AdminPassword123!", "DemoPassword123!", includeDemoData: true);

            await using (var context = _db.CreateContext())
            {
                var employees = await context.Employees.Include(e => e.Department).ToListAsync();
                Assert.Equal(22, await context.Departments.CountAsync());
                Assert.Equal(32, employees.Count);

                var liam = employees.Single(e => e.Email == "liam.johnson@securehr.com");
                Assert.Equal("Customer Service", liam.Department.Name);
                Assert.Equal("Customer Service Representative", liam.JobTitle);
                Assert.Equal("EMP-0003", liam.EmployeeNumber);
            }
        }

        [Fact]
        public async Task Without_demo_data_departments_are_seeded_but_no_employees()
        {
            await using (var context = _db.CreateContext())
                await DatabaseSeeder.SeedAsync(context, "AdminPassword123!", includeDemoData: false);

            await using (var context = _db.CreateContext())
            {
                Assert.False(await context.Employees.AnyAsync());
                Assert.Equal(22, await context.Departments.CountAsync()); // departments are seeded in every environment
                Assert.Equal(["admin"], await context.Users.Select(u => u.Username).ToListAsync()); // no demo users
            }
        }

        [Fact]
        public async Task Seeding_twice_does_not_duplicate_demo_employees()
        {
            for (var i = 0; i < 2; i++)
            {
                await using var context = _db.CreateContext();
                await DatabaseSeeder.SeedAsync(context, "AdminPassword123!", "DemoPassword123!", includeDemoData: true);
            }

            await using (var context = _db.CreateContext())
                Assert.Equal(32, await context.Employees.CountAsync());
        }

        public void Dispose() => _db.Dispose();
    }
}
