using SecureHR.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Contracts;
using System.ComponentModel.DataAnnotations;
using SecureHR.Application.Interfaces.Services;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AuthPolicies.HrStaff)]
    public class PayrollController(IPayrollService payrollService, IReportService reportService) : ControllerBase
    {
        /// <summary>
        /// Get payroll record by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PayrollRecordDto>> GetPayrollById(int id, CancellationToken ct = default)
        {
            var payroll = await payrollService.GetPayrollByIdAsync(id, ct);
            if (payroll == null)
                return NotFound(new { message = "Payroll record not found" });

            return Ok(payroll);
        }

        /// <summary>
        /// Get paged payroll records for an employee
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<PayrollRecordDto>>> GetPayrollByEmployee(
            int employeeId, 
            [FromQuery, Range(1, int.MaxValue)] int page = 1, 
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 10, 
            CancellationToken ct = default)
        {
            var result = await payrollService.GetPayrollByEmployeeAsync(employeeId, page, pageSize, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get payroll records for a period
        /// </summary>
        [HttpGet("period")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PayrollRecordDto>>> GetPayrollByPeriod(
            [FromQuery] DateOnly periodStart,
            [FromQuery] DateOnly periodEnd,
            CancellationToken ct = default)
        {
            if (periodStart > periodEnd)
                return BadRequest(new { message = "Period start must be before or equal to period end" });

            var result = await payrollService.GetPayrollByPeriodAsync(periodStart, periodEnd, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get latest payroll record for an employee
        /// </summary>
        [HttpGet("employee/{employeeId}/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PayrollRecordDto>> GetLatestPayroll(int employeeId, CancellationToken ct = default)
        {
            var result = await payrollService.GetLatestPayrollAsync(employeeId, ct);
            if (result == null)
                return NotFound(new { message = "No payroll records found for this employee" });

            return Ok(result);
        }

        /// <summary>
        /// Get payroll summary for a period
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PayrollSummaryReportDto>> GetPayrollSummary(
            [FromQuery] DateOnly periodStart,
            [FromQuery] DateOnly periodEnd,
            CancellationToken ct = default)
        {
            if (periodStart > periodEnd)
                return BadRequest(new { message = "Period start must be before or equal to period end" });

            var result = await reportService.GetPayrollSummaryAsync(periodStart, periodEnd, ct);
            return Ok(result);
        }

        /// <summary>
        /// Run payroll for one calendar month for all active employees.
        /// Returns 409 if that month was already processed.
        /// </summary>
        [HttpPost("process-batch")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PayrollBatchDto>> ProcessPayrollBatch(
            [FromBody] ProcessPayrollRequestDto request,
            CancellationToken ct = default)
        {
            if (request.PeriodStart > request.PeriodEnd)
                return BadRequest(new { message = "Period start must be before or equal to period end" });

            var result = await payrollService.ProcessPayrollBatchAsync(request, ct);
            return CreatedAtAction(nameof(GetPayrollByPeriod), new { periodStart = request.PeriodStart, periodEnd = request.PeriodEnd }, result);
        }
    }
}
