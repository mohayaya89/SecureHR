using SecureHR.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureHR.Contracts;
using System.ComponentModel.DataAnnotations;
using SecureHR.Application.Interfaces.Repositories;

namespace SecureHR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public class AuditLogController(IAuditLogRepository auditLogRepository) : ControllerBase
    {
        /// <summary>
        /// Get paged audit logs, newest first, optionally filtered
        /// (from, to, entityName, entityId, username, action)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
            [FromQuery] AuditLogFilterRequestDto filter,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await auditLogRepository.GetPagedAsync(filter, page, pageSize, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get audit logs by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogsByUser(
            string userId,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "User ID is required" });

            var result = await auditLogRepository.GetByUserIdAsync(userId, page, pageSize, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get audit logs by entity name
        /// </summary>
        [HttpGet("entity/{entityName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogsByEntity(
            string entityName,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, PagingDefaults.MaxPageSize)] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return BadRequest(new { message = "Entity name is required" });

            var result = await auditLogRepository.GetByEntityAsync(entityName, page, pageSize, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get audit logs by date range
        /// </summary>
        [HttpGet("date-range")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            CancellationToken ct = default)
        {
            if (startDate > endDate)
                return BadRequest(new { message = "Start date must be before or equal to end date" });

            var result = await auditLogRepository.GetByDateRangeAsync(startDate, endDate, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get audit log by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuditLogDto>> GetAuditLogById(int id, CancellationToken ct = default)
        {
            var auditLog = await auditLogRepository.GetByIdAsync(id, ct);
            if (auditLog == null)
                return NotFound(new { message = "Audit log not found" });

            var dto = new AuditLogDto
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId ?? string.Empty,
                Username = auditLog.Username ?? auditLog.UserId ?? string.Empty,
                EntityName = auditLog.EntityName ?? string.Empty,
                EntityId = auditLog.EntityId.ToString(),
                Action = auditLog.Action.ToString(),
                Timestamp = auditLog.Timestamp,
                ChangesJson = auditLog.ChangesJson
            };

            return Ok(dto);
        }
    }
}
