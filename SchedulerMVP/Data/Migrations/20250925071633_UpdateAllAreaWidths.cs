using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAllAreaWidths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update ALL Helplan areas (top level) - more comprehensive
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 50, Level3WidthPercent = 25
                WHERE Name = 'Helplan' OR Name = 'Hel bassäng' OR Name LIKE '%Helplan%' OR Name LIKE '%Hel bassäng%'
            ");

            // Update ALL Halvplan areas (middle level) - more comprehensive
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 50
                WHERE Name LIKE 'Halvplan%' OR Name LIKE 'Halv%' OR Name LIKE '%Halvplan%' OR Name LIKE '%Halv%'
            ");

            // Update ALL Kvartsplan areas (leaf level) - more comprehensive
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 100
                WHERE Name LIKE 'Kvartsplan%' OR Name LIKE 'Bana%' OR Name LIKE '%Kvartsplan%' OR Name LIKE '%Bana%'
            ");

            // Also update any areas that might have default values (100, 100, 100)
            // and set them based on their hierarchical position
            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 50, Level3WidthPercent = 25
                WHERE ParentAreaId IS NULL AND (Level1WidthPercent = 100 AND Level2WidthPercent = 100 AND Level3WidthPercent = 100)
            ");

            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 50
                WHERE ParentAreaId IS NOT NULL 
                AND Id IN (SELECT ParentAreaId FROM Areas WHERE ParentAreaId IS NOT NULL)
                AND (Level1WidthPercent = 100 AND Level2WidthPercent = 100 AND Level3WidthPercent = 100)
            ");

            migrationBuilder.Sql(@"
                UPDATE Areas 
                SET Level1WidthPercent = 100, Level2WidthPercent = 100, Level3WidthPercent = 100
                WHERE ParentAreaId IS NOT NULL 
                AND Id NOT IN (SELECT ParentAreaId FROM Areas WHERE ParentAreaId IS NOT NULL)
                AND (Level1WidthPercent = 100 AND Level2WidthPercent = 100 AND Level3WidthPercent = 100)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
