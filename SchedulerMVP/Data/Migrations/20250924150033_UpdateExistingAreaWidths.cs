using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingAreaWidths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Helplan areas (top level)
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 50, Level3WidthPercent = 25
                WHERE Name = 'Helplan' OR Name = 'Hel bassäng'
            ");

            // Update Halvplan areas (middle level)
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 50
                WHERE Name LIKE 'Halvplan%' OR Name LIKE 'Halv%'
            ");

            // Update Kvartsplan areas (leaf level)
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 100
                WHERE Name LIKE 'Kvartsplan%' OR Name LIKE 'Bana%'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
