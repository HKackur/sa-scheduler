using Microsoft.EntityFrameworkCore.Migrations;

namespace SchedulerMVP.Data.Migrations
{
    public partial class AddGroupType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupType",
                table: "Groups",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            // Backfill nulls with empty to avoid issues in UI filters (kept nullable)
            migrationBuilder.Sql("UPDATE Groups SET GroupType = GroupType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupType",
                table: "Groups");
        }
    }
}


