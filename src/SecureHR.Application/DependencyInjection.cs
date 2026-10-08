using Microsoft.Extensions.DependencyInjection;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Application.Services;

namespace SecureHR.Application
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the business services. They depend on interfaces implemented in
        /// Infrastructure (repositories), registered by AddInfrastructure.
        /// </summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IPayrollService, PayrollService>();
            services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}
