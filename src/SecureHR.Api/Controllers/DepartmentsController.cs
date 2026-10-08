using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Api.Authorization;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
    {
        private const string GetDepartmentRoute = "GetDepartmentById";

        /// <summary>
        /// Get active departments with their active-employee counts
        /// </summary>
        /// <param name="sortBy">"name" (default) or "employeeCount".</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<DepartmentListItemDto>>> GetAllDepartments(
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 10,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool descending = false,
            CancellationToken ct = default)
        {
            var departments = await departmentService.GetPagedAsync(page, pageSize, sortBy, descending, ct);
            return Ok(departments);
        }

        /// <summary>
        /// Get a department by ID
        /// </summary>
        [HttpGet("{id:int}", Name = GetDepartmentRoute)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id, CancellationToken ct = default)
        {
            var department = await departmentService.GetByIdAsync(id, ct);
            return department is null ? NotFound() : Ok(department);
        }

        /// <summary>
        /// Create a new department
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DepartmentDto>> CreateDepartment(
            UpdateDepartmentRequestDto request,
            CancellationToken ct = default)
        {
            var created = await departmentService.CreateAsync(request, ct);
            return CreatedAtRoute(GetDepartmentRoute, new { id = created.Id }, created);
        }

        /// <summary>
        /// Rename a department
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
            int id,
            UpdateDepartmentRequestDto request,
            CancellationToken ct = default)
        {
            return Ok(await departmentService.UpdateAsync(id, request, ct));
        }

        /// <summary>
        /// Deactivate (soft-delete) a department. Refused while it has active employees.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.HrStaff)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteDepartment(
            int id,
            CancellationToken ct = default)
        {
            await departmentService.DeactivateAsync(id, ct);
            return NoContent();
        }
    }
}
