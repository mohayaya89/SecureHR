using SecureHR.Contracts;
using SecureHR.Web.Services;

namespace SecureHR.Web.Client
{
    public class PayrollApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
        : ApiClientBase(httpClient, tokenProvider)
    {
        public async Task<List<PayrollRecordDto>> GetByPeriodAsync(DateOnly periodStart, DateOnly periodEnd) =>
            await GetAsync<List<PayrollRecordDto>>(
                $"api/payroll/period?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}") ?? [];

        /// <summary>Runs payroll for one calendar month.</summary>
        public Task<PayrollBatchDto?> ProcessAsync(DateOnly periodStart, DateOnly periodEnd) =>
            PostAsync<ProcessPayrollRequestDto, PayrollBatchDto>("api/payroll/process-batch",
                new ProcessPayrollRequestDto { PeriodStart = periodStart, PeriodEnd = periodEnd });
    }
}
