using SecureHR.Contracts;
using SecureHR.Application.Exceptions;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Services
{
    public class EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository) : IEmployeeService
    {
        public async Task<PagedResult<EmployeeListItemDto>> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default)
        {
            var (items, totalCount) = await employeeRepository.GetPagedAsync(page, pageSize, sortBy, descending, ct);

            return new PagedResult<EmployeeListItemDto>
            {
                Items = items.Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    EmployeeNumber = e.EmployeeNumber,
                    FullName = $"{e.FirstName} {e.LastName}",
                    JobTitle = e.JobTitle,
                    DepartmentName = e.Department.Name,
                    HireDate = e.HireDate,
                    IsActive = e.IsActive
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var employee = await employeeRepository.GetByIdAsync(id, ct);
            return employee is null ? null : MapToDto(employee);
        }

        public async Task<EmployeeDto> CreateAsync(
            CreateEmployeeRequestDto request,
            string currentUserId,
            CancellationToken ct = default)
        {
            var department = await GetActiveDepartmentAsync(request.DepartmentId, ct);
            await EnsureEmailAvailableAsync(request.Email, null, ct);

            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                JobTitle = request.JobTitle.Trim(),
                DepartmentId = department.Id,
                Department = department,
                HireDate = request.HireDate,
                AnnualSalary = request.AnnualSalary,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await employeeRepository.AddAsync(employee, ct);
            await employeeRepository.SaveChangesAsync(ct);

            return MapToDto(employee);
        }

        public async Task UpdateAsync(
            int id, UpdateEmployeeRequestDto request,
            string currentUserId,
            CancellationToken ct = default)
        {
            var employee = await employeeRepository.GetByIdAsync(id, ct )
            ?? throw new KeyNotFoundException($"Employee {id} not found");

            if (request.DepartmentId != employee.DepartmentId)
            {
                employee.Department = await GetActiveDepartmentAsync(request.DepartmentId, ct);
            }
            await EnsureEmailAvailableAsync(request.Email, id, ct);

            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.Email = request.Email.Trim();
            employee.JobTitle = request.JobTitle.Trim();
            employee.DepartmentId = request.DepartmentId;
            employee.HireDate = request.HireDate;
            employee.AnnualSalary = request.AnnualSalary;
            employee.IsActive = request.IsActive;

            // The entity is tracked, so saving persists the changes
            await employeeRepository.SaveChangesAsync(ct);
        }

        public async Task DeactivateAsync(int id, string currentUserId, CancellationToken ct = default)
        {
            var employee = await employeeRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Employee {id} not found");

            employee.IsActive = false;
            await employeeRepository.SaveChangesAsync(ct);
        }

        private static EmployeeDto MapToDto(Employee e) => new()
        {
            Id = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            JobTitle = e.JobTitle,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name ?? string.Empty,
            HireDate = e.HireDate,
            AnnualSalary = e.AnnualSalary,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt
        };

        private async Task<Department> GetActiveDepartmentAsync(int departmentId, CancellationToken ct)
        {
            var department = await departmentRepository.GetByIdAsync(departmentId, ct);
            if (department is null || !department.IsActive)
                throw new ArgumentException($"Department {departmentId} was not found or is inactive.");
            return department;
        }

        private async Task EnsureEmailAvailableAsync(string email, int? excludeEmployeeId, CancellationToken ct)
        {
            if (await employeeRepository.EmailExistsAsync(email, excludeEmployeeId, ct))
                throw new ConflictException($"An employee with email '{email.Trim()}' already exists.");
        }
    }
}
