using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class FixCalendarBookingSourceTemplateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old constraint if it exists (pointing to BookingTemplates)
            // Note: In SQLite, we need to recreate the table to change foreign keys
            // But EF Core handles this automatically, so we just drop and recreate
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_BookingTemplates_SourceTemplateId",
                table: "CalendarBookings");

            // Drop new constraint if it already exists (to ensure clean state)
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_ScheduleTemplates_SourceTemplateId",
                table: "CalendarBookings");

            // Add the correct constraint pointing to ScheduleTemplates
            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBookings_ScheduleTemplates_SourceTemplateId",
                table: "CalendarBookings",
                column: "SourceTemplateId",
                principalTable: "ScheduleTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_ScheduleTemplates_SourceTemplateId",
                table: "CalendarBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBookings_BookingTemplates_SourceTemplateId",
                table: "CalendarBookings",
                column: "SourceTemplateId",
                principalTable: "BookingTemplates",
                principalColumn: "Id");
        }
    }
}
