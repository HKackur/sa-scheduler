using Microsoft.EntityFrameworkCore.Migrations;

namespace SchedulerMVP.Data.Migrations
{
    public partial class BackfillGroupType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep nullable; only backfill where nulls exist with empty string to avoid issues in UI
            migrationBuilder.Sql(@"UPDATE Groups SET GroupType = '' WHERE GroupType IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert empties back to NULL to be safe
            migrationBuilder.Sql(@"UPDATE Groups SET GroupType = NULL WHERE GroupType = '';");
        }
    }
}

