using Microsoft.EntityFrameworkCore;
using SecureHR.Application.Exceptions;
using SecureHR.Contracts;
using SecureHR.Domain.Enums;

namespace SecureHR.Tests.Services
{
    public class EmployeeServiceTests : IDisposable
    {
        private readonly TestDb _db = new();

        private static CreateEmployeeRequestDto NewRequest(int departmentId, string email = "new.hire@test.local") => new()
        {
            FirstName = "New",
            LastName = "Hire",
            Email = email,
            JobTitle = "  Software Engineer  ",
            DepartmentId = departmentId,
            HireDate = new DateOnly(2026, 1, 15),
            AnnualSalary = 90000m
        };

        [Fact]
        public async Task Create_saves_job_title_and_returns_employee_number_and_department()
        {
            var dept = await _db.AddDepartmentAsync();
            await using var context = _db.CreateContext();

            var created = await _db.EmployeeService(context).CreateAsync(NewRequest(dept.Id), "7");

            Assert.Equal("Software Engineer", created.JobTitle); // trimmed
            Assert.Equal($"EMP-{created.Id:D4}", created.EmployeeNumber);
            Assert.Equal("Engineering", created.DepartmentName);

            var stored = await context.Employees.AsNoTracking().SingleAsync(e => e.Id == created.Id);
            Assert.Equal("Software Engineer", stored.JobTitle);
        }

        [Fact]
        public async Task Create_rejects_duplicate_email_case_insensitively()
        {
            var dept = await _db.AddDepartmentAsync();
            await using var context = _db.CreateContext();
            var service = _db.EmployeeService(context);
            await service.CreateAsync(NewRequest(dept.Id, "dup@test.local"), "7");

            await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(NewRequest(dept.Id, "DUP@test.local"), "7"));
        }

        [Fact]
        public async Task Create_rejects_inactive_department()
        {
            var dept = await _db.AddDepartmentAsync(isActive: false);
            await using var context = _db.CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => _db.EmployeeService(context).CreateAsync(NewRequest(dept.Id), "7"));
        }

        [Fact]
        public async Task Update_saves_all_changes()
        {
            var dept = await _db.AddDepartmentAsync();
            var sales = await _db.AddDepartmentAsync("Sales");
            int id;
            await using (var context = _db.CreateContext())
                id = (await _db.EmployeeService(context).CreateAsync(NewRequest(dept.Id), "7")).Id;

            await using (var context = _db.CreateContext())
            {
                await _db.EmployeeService(context).UpdateAsync(id, new UpdateEmployeeRequestDto
                {
                    FirstName = "Renamed",
                    LastName = "Hire",
                    Email = "renamed@test.local",
                    JobTitle = "Account Executive",
                    DepartmentId = sales.Id,
                    HireDate = new DateOnly(2026, 2, 1),
                    AnnualSalary = 95000m,
                    IsActive = true
                }, "7");
            }

            await using (var context = _db.CreateContext())
            {
                var employee = await _db.EmployeeService(context).GetByIdAsync(id);
                Assert.NotNull(employee);
                Assert.Equal("Renamed", employee.FirstName);
                Assert.Equal("Sales", employee.DepartmentName);
                Assert.Equal(95000m, employee.AnnualSalary);
                Assert.Equal("Account Executive", employee.JobTitle);
            }
        }

        [Fact]
        public async Task Deactivate_persists()
        {
            var dept = await _db.AddDepartmentAsync();
            var employee = await _db.AddEmployeeAsync(dept.Id);

            await using (var context = _db.CreateContext())
                await _db.EmployeeService(context).DeactivateAsync(employee.Id, "7");

            await using (var context = _db.CreateContext())
                Assert.False((await context.Employees.SingleAsync(e => e.Id == employee.Id)).IsActive);
        }

        [Fact]
        public async Task Job_title_change_is_recorded_in_the_audit_log()
        {
            var dept = await _db.AddDepartmentAsync();
            var employee = await _db.AddEmployeeAsync(dept.Id);

            await using (var context = _db.CreateContext())
            {
                await _db.EmployeeService(context).UpdateAsync(employee.Id, new UpdateEmployeeRequestDto
                {
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email,
                    JobTitle = "Team Lead",
                    DepartmentId = employee.DepartmentId,
                    HireDate = employee.HireDate,
                    AnnualSalary = employee.AnnualSalary,
                    IsActive = true
                }, "7");
            }

            await using (var context = _db.CreateContext())
            {
                var update = await context.AuditLogs.SingleAsync(a => a.Action == AuditAction.Update && a.EntityId == employee.Id);
                Assert.Equal("tester", update.Username);
                Assert.Contains("\"JobTitle\":{\"from\":\"Tester\",\"to\":\"Team Lead\"}", update.ChangesJson);
            }
        }

        public void Dispose() => _db.Dispose();
    }
}
