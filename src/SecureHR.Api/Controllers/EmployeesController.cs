using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Api.Authorization;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeesController(
        IEmployeeService employeeService,
        IAuthorizationService authorizationService) : ControllerBase
    {
        private const string GetEmployeeRoute = "GetEmployeeById";

        /// <param name="sortBy">"name" (default), "department" or "hireDate".</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> GetAllAsync(
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool descending = false,
            CancellationToken ct = default)
        {
            var result = await employeeService.GetPagedAsync(page, pageSize, sortBy, descending, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}", Name = GetEmployeeRoute)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var employee = await employeeService.GetByIdAsync(id, ct);
            if (employee == null) return NotFound();

            // Salary is payroll data: only Admin and HR may see it
            if (!(await authorizationService.AuthorizeAsync(User, AuthPolicies.HrStaff)).Succeeded)
                employee.AnnualSalary = null;

            return Ok(employee);
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EmployeeDto>> CreateEmployeeAsync(
            CreateEmployeeRequestDto request,
            CancellationToken ct = default)
        {
            var created = await employeeService.CreateAsync(request, CurrentUserId, ct);
            // Route by name: MVC strips the "Async" suffix from action names, so
            // CreatedAtAction(nameof(GetByIdAsync)) would fail to find the route.
            return CreatedAtRoute(GetEmployeeRoute, new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateEmployeeAsync(
            int id,
            UpdateEmployeeRequestDto request,
            CancellationToken ct = default)
        {
            await employeeService.UpdateAsync(id, request, CurrentUserId, ct);
            return NoContent();
        }

        /// <summary>Deactivates (soft-deletes) an employee.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateEmployeeAsync(
            int id,
            CancellationToken ct = default)
        {
            await employeeService.DeactivateAsync(id, CurrentUserId, ct);
            return NoContent();
        }

        // Always present: every endpoint requires an authenticated user with this claim.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
