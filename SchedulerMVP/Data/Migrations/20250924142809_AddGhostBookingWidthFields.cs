using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGhostBookingWidthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level1WidthPercent",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level2WidthPercent",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level3WidthPercent",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level1WidthPercent",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Level2WidthPercent",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Level3WidthPercent",
                table: "Areas");
        }
    }
}
