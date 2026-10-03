using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureHR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSsnAndBankWithJobTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountEncrypted",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "SsnEncrypted",
                table: "Employee");

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Employee",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Employee");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountEncrypted",
                table: "Employee",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SsnEncrypted",
                table: "Employee",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }
    }
}
