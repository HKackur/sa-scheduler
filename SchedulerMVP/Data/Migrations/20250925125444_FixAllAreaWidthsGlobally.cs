using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAllAreaWidthsGlobally : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all Helplan areas (top level) to have correct width percentages
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 50, Level3WidthPercent = 25
                WHERE ParentAreaId IS NULL AND Name = 'Helplan'
            ");

            // Update all Halvplan areas (middle level) to have correct width percentages
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 50
                WHERE ParentAreaId IS NOT NULL 
                AND Name LIKE 'Halvplan%'
                AND ParentAreaId IN (SELECT Id FROM Areas WHERE ParentAreaId IS NULL AND Name = 'Helplan')
            ");

            // Update all Kvartsplan areas (leaf level) to have correct width percentages
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 100
                WHERE ParentAreaId IS NOT NULL 
                AND (Name LIKE 'Kvartsplan%' OR Name LIKE 'A%' OR Name LIKE 'B%')
                AND ParentAreaId IN (
                    SELECT Id FROM Areas 
                    WHERE ParentAreaId IS NOT NULL 
                    AND Name LIKE 'Halvplan%'
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
