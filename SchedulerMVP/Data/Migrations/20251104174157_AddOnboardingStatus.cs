using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddOnboardingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnboardingCompletedStep",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompletedStep",
                table: "AspNetUsers");
        }
    }
}
