using SecureHR.Contracts;
using SecureHR.Application.Exceptions;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Services
{
    public class DepartmentService(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository) : IDepartmentService
    {
        public async Task<PagedResult<DepartmentListItemDto>> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default)
        {
            var (items, totalCount) = await departmentRepository.GetPagedAsync(page, pageSize, sortBy, descending, ct);
            return new PagedResult<DepartmentListItemDto>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public Task<DepartmentDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return departmentRepository.GetDtoByIdAsync(id, ct);
        }

        public async Task<DepartmentDto> CreateAsync(UpdateDepartmentRequestDto request, CancellationToken ct = default)
        {
            var name = request.Name.Trim();
            if (await departmentRepository.NameExistsAsync(name, null, ct))
                throw new ConflictException($"A department named '{name}' already exists.");

            var department = new Department { Name = name, IsActive = true };
            await departmentRepository.AddAsync(department, ct);
            await departmentRepository.SaveChangesAsync(ct);

            return new DepartmentDto { Id = department.Id, Name = department.Name, IsActive = true, EmployeeCount = 0 };
        }

        public async Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentRequestDto request, CancellationToken ct = default)
        {
            var department = await departmentRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Department {id} not found");

            var name = request.Name.Trim();
            if (await departmentRepository.NameExistsAsync(name, id, ct))
                throw new ConflictException($"A department named '{name}' already exists.");

            department.Name = name;
            await departmentRepository.SaveChangesAsync(ct);

            return (await departmentRepository.GetDtoByIdAsync(id, ct))!;
        }

        public async Task DeactivateAsync(int id, CancellationToken ct = default)
        {
            var department = await departmentRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Department {id} not found");

            if (!department.IsActive)
                return;

            var activeEmployees = await employeeRepository.CountActiveInDepartmentAsync(id, ct);
            if (activeEmployees > 0)
            {
                throw new ConflictException(
                    $"Department '{department.Name}' still has {activeEmployees} active employee(s). " +
                    "Move or deactivate them first.");
            }

            department.IsActive = false;
            await departmentRepository.SaveChangesAsync(ct);
        }
    }
}
