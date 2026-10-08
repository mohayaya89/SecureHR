using SecureHR.Contracts;
using SecureHR.Application.Exceptions;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;
using System.Globalization;

namespace SecureHR.Application.Services
{
    public class PayrollService(IPayrollRecordRepository payrollRepository, IEmployeeRepository employeeRepository) : IPayrollService
    {
        /// <summary>
        /// Placeholder flat rate for taxes and deductions until real rules are modelled.
        /// </summary>
        private const decimal DeductionRate = 0.20m;

        public async Task<PayrollRecordDto?> GetPayrollByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var payroll = await payrollRepository.GetByIdAsync(id, ct);
            if (payroll == null) return null;

            var employee = await employeeRepository.GetByIdAsync(payroll.EmployeeId, ct);
            return MapPayrollDto(payroll, employee?.FirstName, employee?.LastName);
        }

        public async Task<PagedResult<PayrollRecordDto>> GetPayrollByEmployeeAsync(
            int employeeId,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            return await payrollRepository.GetPagedByEmployeeAsync(employeeId, page, pageSize, ct);
        }

        public async Task<IEnumerable<PayrollRecordDto>> GetPayrollByPeriodAsync(
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken ct = default)
        {
            return await payrollRepository.GetByPeriodAsync(periodStart, periodEnd, ct);
        }

        public async Task<PayrollRecordDto?> GetLatestPayrollAsync(
            int employeeId,
            CancellationToken ct = default)
        {
            return await payrollRepository.GetLatestByEmployeeAsync(employeeId, ct);
        }

        /// <summary>
        /// Pays every active employee for one calendar month.
        /// Gross = annual salary / 12, prorated by days employed in the month for mid-month
        /// hires, rounded to cents. Net = gross less the flat <see cref="DeductionRate"/>.
        /// </summary>
        public async Task<PayrollBatchDto> ProcessPayrollBatchAsync(
            ProcessPayrollRequestDto request,
            CancellationToken ct = default)
        {
            var (periodStart, periodEnd) = (request.PeriodStart, request.PeriodEnd);
            var monthName = periodStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            if (periodStart.Day != 1 || periodEnd != periodStart.AddMonths(1).AddDays(-1))
            {
                throw new ArgumentException(
                    "Payroll runs one calendar month at a time: the period must start on the 1st and end on the last day of that month.");
            }

            if (await payrollRepository.ExistsForPeriodAsync(periodStart, periodEnd, ct))
                throw new ConflictException($"Payroll for {monthName} has already been processed.");

            var employees = await employeeRepository.GetAllAsync(ct);
            var daysInMonth = periodEnd.DayNumber - periodStart.DayNumber + 1;

            var result = new PayrollBatchDto { PeriodStart = periodStart, PeriodEnd = periodEnd };
            var records = new List<(PayrollRecord Record, Employee Employee)>();

            foreach (var employee in employees.Where(e => e.IsActive).OrderBy(e => e.FirstName).ThenBy(e => e.LastName))
            {
                var name = $"{employee.FirstName} {employee.LastName}";

                if (employee.HireDate > periodEnd)
                {
                    result.SkippedEmployees.Add($"{name}: hired after {monthName}");
                    continue;
                }

                if (employee.AnnualSalary <= 0)
                {
                    result.SkippedEmployees.Add($"{name}: no annual salary set");
                    continue;
                }

                var firstPaidDay = employee.HireDate > periodStart ? employee.HireDate : periodStart;
                var daysPaid = periodEnd.DayNumber - firstPaidDay.DayNumber + 1;

                var gross = Money(employee.AnnualSalary / 12m * daysPaid / daysInMonth);
                var net = Money(gross * (1 - DeductionRate));

                var record = new PayrollRecord
                {
                    // Link by id only: the employees were loaded untracked, so attaching the
                    // navigation would make EF try to insert them again.
                    EmployeeId = employee.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    GrossSalary = gross,
                    NetSalary = net,
                    ProcessedAt = DateTime.UtcNow
                };

                records.Add((record, employee));
                await payrollRepository.AddAsync(record, ct);
            }

            await payrollRepository.SaveChangesAsync(ct);

            result.Lines = records
                .Select(r => new PayrollBatchLineDto
                {
                    EmployeeId = r.Employee.Id,
                    EmployeeFullName = $"{r.Employee.FirstName} {r.Employee.LastName}",
                    GrossSalary = r.Record.GrossSalary,
                    NetSalary = r.Record.NetSalary
                })
                .ToList();
            result.TotalGross = result.Lines.Sum(l => l.GrossSalary);
            result.TotalNet = result.Lines.Sum(l => l.NetSalary);

            return result;
        }

        private static decimal Money(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        private static PayrollRecordDto MapPayrollDto(PayrollRecord record, string? firstName, string? lastName)
        {
            return new PayrollRecordDto
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeFullName = $"{firstName} {lastName}".Trim(),
                PeriodStart = record.PeriodStart,
                PeriodEnd = record.PeriodEnd,
                GrossSalary = record.GrossSalary,
                NetSalary = record.NetSalary,
                ProcessedAt = record.ProcessedAt
            };
        }
    }
}
