using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeeklyScheduleJson",
                table: "tenants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WeeklyScheduleJson", table: "tenants");
        }
    }
}
