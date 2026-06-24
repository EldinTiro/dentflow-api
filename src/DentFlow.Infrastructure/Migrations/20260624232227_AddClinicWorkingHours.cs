using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkdayStart",
                table: "tenants",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(8, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkdayEnd",
                table: "tenants",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(22, 30));

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WorkdayStart",         table: "tenants");
            migrationBuilder.DropColumn(name: "WorkdayEnd",           table: "tenants");
            migrationBuilder.DropColumn(name: "SlotDurationMinutes",  table: "tenants");
        }
    }
}
