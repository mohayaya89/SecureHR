using SecureHR.Application.Exceptions;
using SecureHR.Contracts;

namespace SecureHR.Tests.Services
{
    public class DepartmentServiceTests : IDisposable
    {
        private readonly TestDb _db = new();

        [Fact]
        public async Task Create_rejects_duplicate_name_case_insensitively()
        {
            await _db.AddDepartmentAsync("Finance");
            await using var context = _db.CreateContext();

            await Assert.ThrowsAsync<ConflictException>(() =>
                _db.DepartmentService(context).CreateAsync(new UpdateDepartmentRequestDto { Name = " finance " }));
        }

        [Fact]
        public async Task Deactivate_is_refused_while_active_employees_remain()
        {
            var dept = await _db.AddDepartmentAsync();
            await _db.AddEmployeeAsync(dept.Id);
            await using var context = _db.CreateContext();

            await Assert.ThrowsAsync<ConflictException>(() => _db.DepartmentService(context).DeactivateAsync(dept.Id));
        }

        [Fact]
        public async Task Deactivated_department_is_hidden_from_the_list()
        {
            var keep = await _db.AddDepartmentAsync("Keep");
            var remove = await _db.AddDepartmentAsync("Remove");
            await _db.AddEmployeeAsync(remove.Id, isActive: false); // inactive employees don't block it

            await using var context = _db.CreateContext();
            var service = _db.DepartmentService(context);
            await service.DeactivateAsync(remove.Id);

            var page = await service.GetPagedAsync(1, 10);
            Assert.Equal([keep.Id], page.Items.Select(d => d.Id));
        }

        [Fact]
        public async Task List_counts_only_active_employees_and_sorts()
        {
            var big = await _db.AddDepartmentAsync("Big");
            var small = await _db.AddDepartmentAsync("Small");
            await _db.AddEmployeeAsync(big.Id, "A");
            await _db.AddEmployeeAsync(big.Id, "B");
            await _db.AddEmployeeAsync(small.Id, "C");
            await _db.AddEmployeeAsync(small.Id, "D", isActive: false);

            await using var context = _db.CreateContext();
            var page = await _db.DepartmentService(context).GetPagedAsync(1, 10, "employeeCount", descending: true);

            Assert.Equal(["Big", "Small"], page.Items.Select(d => d.Name));
            Assert.Equal([2, 1], page.Items.Select(d => d.EmployeeCount));
        }

        public void Dispose() => _db.Dispose();
    }
}
