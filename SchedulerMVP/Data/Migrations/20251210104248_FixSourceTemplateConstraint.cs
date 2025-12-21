using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class FixSourceTemplateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, drop old constraint if it exists (pointing to BookingTemplates)
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_BookingTemplates_SourceTemplateId",
                table: "CalendarBookings");

            // Drop new constraint if it already exists (to ensure clean state)
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_ScheduleTemplates_SourceTemplateId",
                table: "CalendarBookings");
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Groups_GroupId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_Groups_GroupId",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "BookingTitle",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "IsOtherBookingType",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "BookingTitle",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsOtherBookingType",
                table: "Bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Groups_GroupId",
                table: "Bookings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBookings_Groups_GroupId",
                table: "CalendarBookings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Add the correct constraint pointing to ScheduleTemplates (not BookingTemplates)
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
                name: "FK_Bookings_Groups_GroupId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_Groups_GroupId",
                table: "CalendarBookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "BookingTitle",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOtherBookingType",
                table: "CalendarBookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Bookings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "BookingTitle",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOtherBookingType",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Groups_GroupId",
                table: "Bookings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBookings_Groups_GroupId",
                table: "CalendarBookings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            // Restore old constraint pointing to BookingTemplates (for rollback)
            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBookings_BookingTemplates_SourceTemplateId",
                table: "CalendarBookings",
                column: "SourceTemplateId",
                principalTable: "BookingTemplates",
                principalColumn: "Id");
        }
    }
}
