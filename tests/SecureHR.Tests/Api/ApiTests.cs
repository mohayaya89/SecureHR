using SecureHR.Contracts;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SecureHR.Tests.Api
{
    public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Login_with_wrong_password_returns_401()
        {
            var response = await factory.CreateClient().PostAsJsonAsync("api/auth/login",
                new LoginRequestDto { Username = "admin", Password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("api/employees")]
        [InlineData("api/departments")]
        [InlineData("api/payroll/period?periodStart=2026-09-01&periodEnd=2026-09-30")]
        [InlineData("api/auditlog")]
        public async Task Endpoints_require_login(string url)
        {
            var response = await factory.CreateClient().GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        // ReadOnly: may read employees and headcount, nothing payroll- or audit-related
        [InlineData("viewer", "api/employees", HttpStatusCode.OK)]
        [InlineData("viewer", "api/reports/headcount", HttpStatusCode.OK)]
        [InlineData("viewer", "api/payroll/period?periodStart=2026-09-01&periodEnd=2026-09-30", HttpStatusCode.Forbidden)]
        [InlineData("viewer", "api/auditlog", HttpStatusCode.Forbidden)]
        // HR: payroll, but not the audit log
        [InlineData("hrstaff", "api/payroll/period?periodStart=2026-09-01&periodEnd=2026-09-30", HttpStatusCode.OK)]
        [InlineData("hrstaff", "api/auditlog", HttpStatusCode.Forbidden)]
        // Admin: everything
        [InlineData("admin", "api/auditlog", HttpStatusCode.OK)]
        public async Task Role_rules_are_enforced(string user, string url, HttpStatusCode expected)
        {
            var password = user == "admin" ? ApiFactory.AdminPassword : ApiFactory.DemoPassword;
            var client = await factory.CreateClientAsync(user, password);

            var response = await client.GetAsync(url);

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task Viewer_cannot_create_employees()
        {
            var viewer = await factory.ViewerAsync();

            var response = await viewer.PostAsJsonAsync("api/employees", NewEmployee("viewer.attempt@test.local"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Salary_is_hidden_from_read_only_users()
        {
            var hr = await (await factory.HrAsync()).GetFromJsonAsync<EmployeeDto>("api/employees/1");
            var viewer = await (await factory.ViewerAsync()).GetFromJsonAsync<EmployeeDto>("api/employees/1");

            Assert.NotNull(hr!.AnnualSalary);
            Assert.Null(viewer!.AnnualSalary);
        }

        [Fact]
        public async Task Create_returns_201_and_duplicate_email_returns_409_problem_details()
        {
            var hr = await factory.HrAsync();

            var created = await hr.PostAsJsonAsync("api/employees", NewEmployee("api.test@test.local"));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.NotNull(created.Headers.Location);

            var duplicate = await hr.PostAsJsonAsync("api/employees", NewEmployee("API.TEST@test.local"));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
            var problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("already exists", problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Invalid_input_returns_400_with_field_errors()
        {
            var hr = await factory.HrAsync();
            var request = NewEmployee("not-an-email");
            request.JobTitle = "";

            var response = await hr.PostAsJsonAsync("api/employees", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
            Assert.True(errors.TryGetProperty("Email", out _));
            Assert.True(errors.TryGetProperty("JobTitle", out _));
        }

        [Fact]
        public async Task Page_size_is_capped()
        {
            var viewer = await factory.ViewerAsync();

            var response = await viewer.GetAsync($"api/employees?pageSize={PagingDefaults.MaxPageSize + 1}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Changes_made_through_the_api_appear_in_the_audit_log()
        {
            var hr = await factory.HrAsync();
            var response = await hr.PostAsJsonAsync("api/employees", NewEmployee("audited@test.local"));
            var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();

            var admin = await factory.AdminAsync();
            var logs = await admin.GetFromJsonAsync<PagedResult<AuditLogDto>>(
                $"api/auditlog?entityName=Employee&entityId={created!.Id}&username=hrstaff");

            var entry = Assert.Single(logs!.Items);
            Assert.Equal("Create", entry.Action);
        }

        private static CreateEmployeeRequestDto NewEmployee(string email) => new()
        {
            FirstName = "Api",
            LastName = "Test",
            Email = email,
            JobTitle = "QA Engineer",
            DepartmentId = 1,
            HireDate = new DateOnly(2026, 1, 15),
            AnnualSalary = 80000m
        };
    }
}
