using SecureHR.Contracts;
using SecureHR.Web.Services;

namespace SecureHR.Web.Client
{
    public class ReportApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
        : ApiClientBase(httpClient, tokenProvider)
    {
        public async Task<List<HeadcountByDepartmentDto>> GetHeadcountAsync() =>
            await GetAsync<List<HeadcountByDepartmentDto>>("api/reports/headcount") ?? [];

        public Task<PayrollSummaryReportDto?> GetPayrollSummaryAsync(DateOnly periodStart, DateOnly periodEnd) =>
            GetAsync<PayrollSummaryReportDto>($"api/reports/payroll-summary?{Period(periodStart, periodEnd)}");

        public async Task<List<SalaryDistributionPointDto>> GetSalaryDistributionAsync(DateOnly periodStart, DateOnly periodEnd) =>
            await GetAsync<List<SalaryDistributionPointDto>>($"api/reports/salary-distribution?{Period(periodStart, periodEnd)}") ?? [];

        private static string Period(DateOnly start, DateOnly end) =>
            $"periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}";
    }
}
