using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartMin = table.Column<int>(type: "INTEGER", nullable: false),
                    EndMin = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SourceTemplateId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarBookings_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalendarBookings_BookingTemplates_SourceTemplateId",
                        column: x => x.SourceTemplateId,
                        principalTable: "BookingTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarBookings_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_AreaId",
                table: "CalendarBookings",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_GroupId",
                table: "CalendarBookings",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_SourceTemplateId",
                table: "CalendarBookings",
                column: "SourceTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarBookings");
        }
    }
}
