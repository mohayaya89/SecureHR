using SecureHR.Contracts;
using SecureHR.Web.Services;

namespace SecureHR.Web.Client;

public class DepartmentApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
    : ApiClientBase(httpClient, tokenProvider)
{
    /// <param name="sortBy">"name" or "employeeCount".</param>
    public Task<PagedResult<DepartmentListItemDto>?> GetPagedAsync(
        int page = 1, int pageSize = 20, string sortBy = "name", bool descending = false) =>
        GetAsync<PagedResult<DepartmentListItemDto>>(
            $"api/departments?page={page}&pageSize={pageSize}&sortBy={Uri.EscapeDataString(sortBy)}&descending={descending}");

    public Task<DepartmentDto?> CreateAsync(CreateDepartmentRequestDto request) =>
        PostAsync<CreateDepartmentRequestDto, DepartmentDto>("api/departments", request);

    public Task UpdateAsync(int id, UpdateDepartmentRequestDto request) =>
        PutAsync($"api/departments/{id}", request);

    public Task DeactivateAsync(int id) =>
        DeleteAsync($"api/departments/{id}");
}
