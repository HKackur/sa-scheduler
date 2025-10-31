using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot_GroupType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupType",
                table: "Groups",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupType",
                table: "Groups");
        }
    }
}
