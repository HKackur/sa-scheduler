using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Migrations;

/// <inheritdoc />
public partial class AddClubIdToUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add ClubId to AspNetUsers
        // Note: Club.Id is Guid, stored as TEXT in SQLite
        migrationBuilder.AddColumn<string>(
            name: "ClubId",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "AspNetUsers");
    }
}

