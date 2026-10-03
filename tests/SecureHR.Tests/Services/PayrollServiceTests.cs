using SecureHR.Application.Exceptions;
using SecureHR.Contracts;

namespace SecureHR.Tests.Services
{
    public class PayrollServiceTests : IDisposable
    {
        private static readonly ProcessPayrollRequestDto September2026 =
            new() { PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 30) };

        private readonly TestDb _db = new();

        [Fact]
        public async Task Pays_monthly_salary_rounded_to_cents_with_flat_deduction()
        {
            var dept = await _db.AddDepartmentAsync();
            await _db.AddEmployeeAsync(dept.Id, "Full", annualSalary: 95000m);
            await using var context = _db.CreateContext();

            var result = await _db.PayrollService(context).ProcessPayrollBatchAsync(September2026);

            var line = Assert.Single(result.Lines);
            Assert.Equal(7916.67m, line.GrossSalary);   // 95,000 / 12
            Assert.Equal(6333.34m, line.NetSalary);     // 80% of gross
        }

        [Fact]
        public async Task Prorates_mid_month_hire_and_skips_ineligible_employees()
        {
            var dept = await _db.AddDepartmentAsync();
            await _db.AddEmployeeAsync(dept.Id, "Mid", annualSalary: 60000m, hireDate: new DateOnly(2026, 9, 15));
            await _db.AddEmployeeAsync(dept.Id, "Future", hireDate: new DateOnly(2026, 10, 1));
            await _db.AddEmployeeAsync(dept.Id, "Unpaid", annualSalary: 0m);
            await _db.AddEmployeeAsync(dept.Id, "Gone", isActive: false);
            await using var context = _db.CreateContext();

            var result = await _db.PayrollService(context).ProcessPayrollBatchAsync(September2026);

            var line = Assert.Single(result.Lines);
            Assert.Equal(2666.67m, line.GrossSalary);   // 5,000 x 16/30 days
            Assert.Equal(2, result.SkippedEmployees.Count);
            Assert.Contains(result.SkippedEmployees, s => s.StartsWith("Future") && s.Contains("hired after"));
            Assert.Contains(result.SkippedEmployees, s => s.StartsWith("Unpaid") && s.Contains("no annual salary"));
        }

        [Fact]
        public async Task Same_month_cannot_be_processed_twice()
        {
            var dept = await _db.AddDepartmentAsync();
            await _db.AddEmployeeAsync(dept.Id);
            await using var context = _db.CreateContext();
            var service = _db.PayrollService(context);
            await service.ProcessPayrollBatchAsync(September2026);

            await Assert.ThrowsAsync<ConflictException>(() => service.ProcessPayrollBatchAsync(September2026));
        }

        [Theory]
        [InlineData("2026-09-02", "2026-09-30")] // doesn't start on the 1st
        [InlineData("2026-09-01", "2026-09-29")] // doesn't end on the last day
        [InlineData("2026-09-01", "2026-10-31")] // more than one month
        public async Task Period_must_be_one_calendar_month(string start, string end)
        {
            await using var context = _db.CreateContext();
            var request = new ProcessPayrollRequestDto { PeriodStart = DateOnly.Parse(start), PeriodEnd = DateOnly.Parse(end) };

            await Assert.ThrowsAsync<ArgumentException>(() => _db.PayrollService(context).ProcessPayrollBatchAsync(request));
        }

        public void Dispose() => _db.Dispose();
    }
}
