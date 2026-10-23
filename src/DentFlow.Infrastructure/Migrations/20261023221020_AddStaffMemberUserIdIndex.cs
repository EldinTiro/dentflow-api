using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMemberUserIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_staff_members_TenantId_UserId",
                table: "staff_members",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_staff_members_TenantId_UserId",
                table: "staff_members");
        }
    }
}
