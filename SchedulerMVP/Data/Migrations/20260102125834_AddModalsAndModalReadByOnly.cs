using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class AddModalsAndModalReadByOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Groups_GroupId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBookings_Groups_GroupId",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "BookingTitle",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsOtherBookingType",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "IsOtherBookingType",
                table: "CalendarBookings",
                newName: "Published");

            migrationBuilder.RenameColumn(
                name: "BookingTitle",
                table: "CalendarBookings",
                newName: "PublishedAt");

            migrationBuilder.AddColumn<string>(
                name: "StandardDisplayColor",
                table: "GroupTypes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayColor",
                table: "Groups",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Groups",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.CreateTable(
                name: "Modals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    LinkRoute = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ButtonText = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModalReadBy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModalReadBy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModalReadBy_Modals_ModalId",
                        column: x => x.ModalId,
                        principalTable: "Modals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_UserId",
                table: "ScheduleTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_UserId",
                table: "Places",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_UserId",
                table: "Groups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_Date",
                table: "CalendarBookings",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_Date_AreaId",
                table: "CalendarBookings",
                columns: new[] { "Date", "AreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModalReadBy_ModalId_UserId",
                table: "ModalReadBy",
                columns: new[] { "ModalId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Modals_StartDate_EndDate",
                table: "Modals",
                columns: new[] { "StartDate", "EndDate" });

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

            migrationBuilder.DropTable(
                name: "ModalReadBy");

            migrationBuilder.DropTable(
                name: "Modals");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleTemplates_UserId",
                table: "ScheduleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Places_UserId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Groups_UserId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_Date",
                table: "CalendarBookings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_Date_AreaId",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "StandardDisplayColor",
                table: "GroupTypes");

            migrationBuilder.DropColumn(
                name: "DisplayColor",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Groups");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                table: "CalendarBookings",
                newName: "BookingTitle");

            migrationBuilder.RenameColumn(
                name: "Published",
                table: "CalendarBookings",
                newName: "IsOtherBookingType");

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

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
        }
    }
}
