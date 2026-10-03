using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Application.Services;
using SecureHR.Domain.Entities;
using SecureHR.Infrastructure.Data;
using SecureHR.Infrastructure.Repository;

namespace SecureHR.Tests
{
    /// <summary>
    /// An in-memory SQLite database built from the real migrations, with the audit
    /// interceptor attached. SQLite (unlike EF's InMemory provider) enforces unique
    /// indexes and transactions, which several tests depend on.
    /// </summary>
    public sealed class TestDb : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public FakeCurrentUser User { get; } = new();

        public TestDb()
        {
            _connection.Open();
            using var context = CreateContext();
            context.Database.Migrate();
        }

        public SecureHRDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<SecureHRDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new AuditSaveChangesInterceptor(User))
                .Options);

        public void Execute(string sql)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public EmployeeService EmployeeService(SecureHRDbContext context) =>
            new(new EmployeeRepository(context), new DepartmentRepository(context));

        public DepartmentService DepartmentService(SecureHRDbContext context) =>
            new(new DepartmentRepository(context), new EmployeeRepository(context));

        public PayrollService PayrollService(SecureHRDbContext context) =>
            new(new PayrollRecordRepository(context), new EmployeeRepository(context));

        public async Task<Department> AddDepartmentAsync(string name = "Engineering", bool isActive = true)
        {
            await using var context = CreateContext();
            var department = new Department { Name = name, IsActive = isActive };
            context.Departments.Add(department);
            await context.SaveChangesAsync();
            return department;
        }

        public async Task<Employee> AddEmployeeAsync(
            int departmentId,
            string firstName = "Ada",
            decimal annualSalary = 120000m,
            DateOnly? hireDate = null,
            bool isActive = true)
        {
            await using var context = CreateContext();
            var employee = new Employee
            {
                FirstName = firstName,
                LastName = "Test",
                Email = $"{firstName.ToLowerInvariant()}.{Guid.NewGuid():N}@test.local",
                JobTitle = "Tester",
                DepartmentId = departmentId,
                HireDate = hireDate ?? new DateOnly(2020, 1, 1),
                AnnualSalary = annualSalary,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        public void Dispose() => _connection.Dispose();
    }

    public sealed class FakeCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; } = "7";
        public string? Username { get; set; } = "tester";
    }
}
