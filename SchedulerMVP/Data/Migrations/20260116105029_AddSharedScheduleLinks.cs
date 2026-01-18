using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedScheduleLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedScheduleLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduleTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShareToken = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowWeekView = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowDayView = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowListView = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AllowBookingRequests = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedScheduleLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedScheduleLinks_ScheduleTemplates_ScheduleTemplateId",
                        column: x => x.ScheduleTemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Critical index for O(1) token lookup - must be unique
            migrationBuilder.CreateIndex(
                name: "IX_SharedScheduleLinks_ShareToken",
                table: "SharedScheduleLinks",
                column: "ShareToken",
                unique: true);

            // Index for template lookup
            migrationBuilder.CreateIndex(
                name: "IX_SharedScheduleLinks_ScheduleTemplateId",
                table: "SharedScheduleLinks",
                column: "ScheduleTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedScheduleLinks_ScheduleTemplateId",
                table: "SharedScheduleLinks");

            migrationBuilder.DropIndex(
                name: "IX_SharedScheduleLinks_ShareToken",
                table: "SharedScheduleLinks");

            migrationBuilder.DropTable(
                name: "SharedScheduleLinks");
        }
    }
}
