using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index on Groups.UserId for faster user filtering
            migrationBuilder.CreateIndex(
                name: "IX_Groups_UserId",
                table: "Groups",
                column: "UserId");

            // Index on Places.UserId for faster user filtering
            migrationBuilder.CreateIndex(
                name: "IX_Places_UserId",
                table: "Places",
                column: "UserId");

            // Index on ScheduleTemplates.UserId for faster user filtering
            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_UserId",
                table: "ScheduleTemplates",
                column: "UserId");

            // Index on CalendarBookings.Date for faster week queries
            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_Date",
                table: "CalendarBookings",
                column: "Date");

            // Composite index on CalendarBookings for common query patterns (Date + AreaId)
            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_Date_AreaId",
                table: "CalendarBookings",
                columns: new[] { "Date", "AreaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_UserId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Places_UserId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleTemplates_UserId",
                table: "ScheduleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_Date",
                table: "CalendarBookings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_Date_AreaId",
                table: "CalendarBookings");
        }
    }
}

