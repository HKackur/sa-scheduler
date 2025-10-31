using Microsoft.EntityFrameworkCore.Migrations;

namespace SchedulerMVP.Data.Migrations
{
    public partial class RenameHerrUToP19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename group name Herr U -> P19
            migrationBuilder.Sql(@"UPDATE Groups SET Name = 'P19' WHERE LOWER(Name) = 'herr u';");
            // Update type to Akademi where currently U-Herr or null/empty
            migrationBuilder.Sql(@"UPDATE Groups SET GroupType = 'Akademi' WHERE LOWER(Name) = 'p19';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert
            migrationBuilder.Sql(@"UPDATE Groups SET Name = 'Herr U' WHERE LOWER(Name) = 'p19';");
            migrationBuilder.Sql(@"UPDATE Groups SET GroupType = 'U-Herr' WHERE LOWER(Name) = 'herr u';");
        }
    }
}

