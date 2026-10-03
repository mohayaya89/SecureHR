using SecureHR.Contracts;
using SecureHR.Web.Services;

namespace SecureHR.Web.Client
{
    public class EmployeeApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
        : ApiClientBase(httpClient, tokenProvider)
    {
        /// <param name="sortBy">"name", "department" or "hireDate".</param>
        public Task<PagedResult<EmployeeListItemDto>?> GetPagedAsync(
            int page = 1, int pageSize = 20, string sortBy = "name", bool descending = false) =>
            GetAsync<PagedResult<EmployeeListItemDto>>(
                $"api/employees?page={page}&pageSize={pageSize}&sortBy={Uri.EscapeDataString(sortBy)}&descending={descending}");

        public Task<EmployeeDto?> GetByIdAsync(int id) =>
            GetAsync<EmployeeDto>($"api/employees/{id}");

        public Task UpdateAsync(int id, UpdateEmployeeRequestDto request) =>
            PutAsync($"api/employees/{id}", request);

        public Task<EmployeeDto?> CreateAsync(CreateEmployeeRequestDto request) =>
            PostAsync<CreateEmployeeRequestDto, EmployeeDto>("api/employees", request);

        public Task DeactivateAsync(int id) =>
            DeleteAsync($"api/employees/{id}");
    }
}
