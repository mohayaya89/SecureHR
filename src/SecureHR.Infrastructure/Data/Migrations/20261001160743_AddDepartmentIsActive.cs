using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureHR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Department",
                type: "INTEGER",
                nullable: false,
                defaultValue: true); // existing departments stay active
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Department");
        }
    }
}
