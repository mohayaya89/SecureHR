using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureHR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditAndPayrollRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualSalary",
                table: "Employee",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AuditLog",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            // Older builds allowed paying an employee twice for the same period; keep the
            // earliest record so the unique index below can be created.
            migrationBuilder.Sql(
                """
                DELETE FROM "PayrollRecord"
                WHERE "Id" NOT IN (
                    SELECT MIN("Id") FROM "PayrollRecord"
                    GROUP BY "EmployeeId", "PeriodStart", "PeriodEnd");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecord_EmployeeId_PeriodStart_PeriodEnd",
                table: "PayrollRecord",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollRecord_EmployeeId_PeriodStart_PeriodEnd",
                table: "PayrollRecord");

            migrationBuilder.DropColumn(
                name: "AnnualSalary",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AuditLog");
        }
    }
}
