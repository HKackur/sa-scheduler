using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardDisplayColorToGroupType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StandardDisplayColor",
                table: "GroupTypes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Ljusblå");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandardDisplayColor",
                table: "GroupTypes");
        }
    }
}
