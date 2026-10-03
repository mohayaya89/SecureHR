using SecureHR.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Services;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController(IReportService reportService) : ControllerBase
    {
        /// <summary>
        /// Get payroll summary for a period
        /// </summary>
        [HttpGet("payroll-summary")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
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
        /// Get department payroll summary for a period
        /// </summary>
        [HttpGet("department-payroll-summary")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DepartmentPayrollSummaryDto>>> GetDepartmentPayrollSummary(
            [FromQuery] DateOnly periodStart,
            [FromQuery] DateOnly periodEnd,
            CancellationToken ct = default)
        {
            if (periodStart > periodEnd)
                return BadRequest(new { message = "Period start must be before or equal to period end" });

            var result = await reportService.GetDepartmentPayrollSummaryAsync(periodStart, periodEnd, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get headcount report by department
        /// </summary>
        [HttpGet("headcount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<HeadcountByDepartmentDto>>> GetHeadcountReport(CancellationToken ct = default)
        {
            var result = await reportService.GetHeadcountReportAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Get employees by department
        /// </summary>
        [HttpGet("department/{departmentId}/employees")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EmployeeListItemDto>>> GetEmployeesByDepartment(
            int departmentId,
            CancellationToken ct = default)
        {
            var result = await reportService.GetEmployeesByDepartmentAsync(departmentId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get salary distribution in salary ranges (buckets) for a period
        /// </summary>
        [HttpGet("salary-distribution")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SalaryDistributionPointDto>>> GetSalaryDistribution(
            [FromQuery] DateOnly periodStart,
            [FromQuery] DateOnly periodEnd,
            CancellationToken ct = default)
        {
            if (periodStart > periodEnd)
                return BadRequest(new { message = "Period start must be before or equal to period end" });

            var result = await reportService.GetSalaryDistributionAsync(periodStart, periodEnd, ct);
            return Ok(result);
        }
    }
}
