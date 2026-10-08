using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Enums;
using SecureHR.Infrastructure.Cryptography;
using System.Text.Json;

namespace SecureHR.Tests.Infrastructure
{
    public class AuditTrailTests : IDisposable
    {
        private readonly TestDb _db = new();

        [Fact]
        public async Task Create_is_audited_with_user_and_real_entity_id()
        {
            var dept = await _db.AddDepartmentAsync();

            await using var context = _db.CreateContext();
            var log = await context.AuditLogs.SingleAsync();
            Assert.Equal(AuditAction.Create, log.Action);
            Assert.Equal("Department", log.EntityName);
            Assert.Equal(dept.Id, log.EntityId);
            Assert.Equal("tester", log.Username);
            Assert.Equal("7", log.UserId);
        }

        [Fact]
        public async Task Update_records_only_changed_fields()
        {
            var dept = await _db.AddDepartmentAsync();
            var employee = await _db.AddEmployeeAsync(dept.Id, annualSalary: 50000m);

            await using (var context = _db.CreateContext())
            {
                var entity = await context.Employees.SingleAsync(e => e.Id == employee.Id);
                entity.AnnualSalary = 55000m;
                entity.JobTitle = "Team Lead";
                await context.SaveChangesAsync();
            }

            await using (var context = _db.CreateContext())
            {
                var log = await context.AuditLogs.SingleAsync(a => a.Action == AuditAction.Update);
                using var changes = JsonDocument.Parse(log.ChangesJson!);
                var fields = changes.RootElement.EnumerateObject().Select(p => p.Name).Order().ToList();

                Assert.Equal(["AnnualSalary", "JobTitle"], fields);
                Assert.Equal("Team Lead", changes.RootElement.GetProperty("JobTitle").GetProperty("to").GetString());
            }
        }

        [Fact]
        public async Task Password_hash_changes_are_recorded_without_the_value()
        {
            var hash = PasswordHashing.Hash("OldPassword1!");
            var newHash = PasswordHashing.Hash("NewPassword1!");
            int userId;
            await using (var context = _db.CreateContext())
            {
                var user = new User { Username = "audited", PasswordHash = hash, Role = UserRole.HR };
                context.Users.Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;
            }

            await using (var context = _db.CreateContext())
            {
                (await context.Users.SingleAsync(u => u.Id == userId)).PasswordHash = newHash;
                await context.SaveChangesAsync();
            }

            await using (var context = _db.CreateContext())
            {
                var logs = await context.AuditLogs.Where(a => a.EntityName == "User").ToListAsync();
                var update = Assert.Single(logs, a => a.Action == AuditAction.Update);
                using var changes = JsonDocument.Parse(update.ChangesJson!);
                Assert.Equal("(changed)", changes.RootElement.GetProperty("PasswordHash").GetString());
                Assert.All(logs, log => Assert.DoesNotContain(hash, log.ChangesJson ?? ""));
                Assert.All(logs, log => Assert.DoesNotContain(newHash, log.ChangesJson ?? ""));
            }
        }

        [Fact]
        public async Task Deactivation_is_audited_as_delete()
        {
            var dept = await _db.AddDepartmentAsync();
            var employee = await _db.AddEmployeeAsync(dept.Id);

            await using (var context = _db.CreateContext())
            {
                (await context.Employees.SingleAsync(e => e.Id == employee.Id)).IsActive = false;
                await context.SaveChangesAsync();
            }

            await using (var context = _db.CreateContext())
            {
                var deletes = await context.AuditLogs
                    .Where(a => a.Action == AuditAction.Delete && a.EntityId == employee.Id)
                    .ToListAsync();
                Assert.Single(deletes);
            }
        }

        [Fact]
        public async Task Change_is_rolled_back_if_its_audit_record_cannot_be_written()
        {
            _db.Execute(
                "CREATE TRIGGER fail_audit BEFORE INSERT ON \"AuditLog\" " +
                "BEGIN SELECT RAISE(ABORT, 'audit unavailable'); END;");

            await Assert.ThrowsAnyAsync<Exception>(() => _db.AddDepartmentAsync("Unaudited"));

            await using var context = _db.CreateContext();
            Assert.False(await context.Departments.AnyAsync(d => d.Name == "Unaudited"));
        }

        [Fact]
        public async Task Failed_save_writes_no_audit_record()
        {
            await _db.AddDepartmentAsync("Unique");
            var before = await CountAuditRowsAsync();

            // Violates the unique index on department name
            await Assert.ThrowsAsync<DbUpdateException>(() => _db.AddDepartmentAsync("Unique"));

            Assert.Equal(before, await CountAuditRowsAsync());
        }

        private async Task<int> CountAuditRowsAsync()
        {
            await using var context = _db.CreateContext();
            return await context.AuditLogs.CountAsync();
        }

        public void Dispose() => _db.Dispose();
    }
}
