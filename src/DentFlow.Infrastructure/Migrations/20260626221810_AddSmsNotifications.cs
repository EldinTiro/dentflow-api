using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReminderSlot = table.Column<byte>(type: "smallint", nullable: false),
                    ToPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MessageBody = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_notification_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder1HoursBefore = table.Column<int>(type: "integer", nullable: true),
                    Reminder2HoursBefore = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_notification_configs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_AppointmentId_ReminderSlot",
                table: "notification_logs",
                columns: new[] { "AppointmentId", "ReminderSlot" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_ProviderMessageId",
                table: "notification_logs",
                column: "ProviderMessageId",
                unique: true,
                filter: "\"ProviderMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_notification_configs_TenantId",
                table: "tenant_notification_configs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "tenant_notification_configs");
        }
    }
}
