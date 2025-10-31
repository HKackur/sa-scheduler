using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "BookingTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "BookingTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "BookingTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "BookingTemplates");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "BookingTemplates");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "BookingTemplates");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "CalendarBookings");
        }
    }
}
