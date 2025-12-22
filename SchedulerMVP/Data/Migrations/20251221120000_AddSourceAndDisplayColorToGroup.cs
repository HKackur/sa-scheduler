using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceAndDisplayColorToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Groups",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Egen");

            migrationBuilder.AddColumn<string>(
                name: "DisplayColor",
                table: "Groups",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Ljusblå");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "DisplayColor",
                table: "Groups");
        }
    }
}
