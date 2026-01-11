using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerMVP.Data.Migrations;

/// <inheritdoc />
public partial class AddClubsAndClubId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Clubs table
        migrationBuilder.CreateTable(
            name: "Clubs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clubs", x => x.Id);
            });

        // Add ClubId to AspNetUsers (ApplicationUser)
        // Note: AspNetUsers is in ApplicationDbContext, but we add the column here via SQL
        // We handle the relationship in application code (no FK constraint)
        // ApplicationUser.ClubId is Guid?, so we store as TEXT (SQLite stores Guid as TEXT)
        migrationBuilder.Sql(@"
            ALTER TABLE AspNetUsers 
            ADD COLUMN ClubId TEXT;
        ");

        // Add ClubId to Places
        migrationBuilder.AddColumn<Guid>(
            name: "ClubId",
            table: "Places",
            type: "TEXT",
            nullable: true);

        // Add ClubId to Groups
        migrationBuilder.AddColumn<Guid>(
            name: "ClubId",
            table: "Groups",
            type: "TEXT",
            nullable: true);

        // Add ClubId to ScheduleTemplates
        migrationBuilder.AddColumn<Guid>(
            name: "ClubId",
            table: "ScheduleTemplates",
            type: "TEXT",
            nullable: true);

        // Add ClubId to GroupTypes
        migrationBuilder.AddColumn<Guid>(
            name: "ClubId",
            table: "GroupTypes",
            type: "TEXT",
            nullable: true);

        // Create indexes for ClubId
        migrationBuilder.CreateIndex(
            name: "IX_Places_ClubId",
            table: "Places",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_Groups_ClubId",
            table: "Groups",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_ScheduleTemplates_ClubId",
            table: "ScheduleTemplates",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupTypes_ClubId",
            table: "GroupTypes",
            column: "ClubId");

        // Add foreign keys
        migrationBuilder.AddForeignKey(
            name: "FK_Places_Clubs_ClubId",
            table: "Places",
            column: "ClubId",
            principalTable: "Clubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_Groups_Clubs_ClubId",
            table: "Groups",
            column: "ClubId",
            principalTable: "Clubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_ScheduleTemplates_Clubs_ClubId",
            table: "ScheduleTemplates",
            column: "ClubId",
            principalTable: "Clubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_GroupTypes_Clubs_ClubId",
            table: "GroupTypes",
            column: "ClubId",
            principalTable: "Clubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // Note: AspNetUsers.ClubId does not have a foreign key constraint
        // because ApplicationUser is in ApplicationDbContext and Club is in AppDbContext
        // We'll handle this relationship in application code
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop foreign keys
        migrationBuilder.DropForeignKey(
            name: "FK_GroupTypes_Clubs_ClubId",
            table: "GroupTypes");

        migrationBuilder.DropForeignKey(
            name: "FK_ScheduleTemplates_Clubs_ClubId",
            table: "ScheduleTemplates");

        migrationBuilder.DropForeignKey(
            name: "FK_Groups_Clubs_ClubId",
            table: "Groups");

        migrationBuilder.DropForeignKey(
            name: "FK_Places_Clubs_ClubId",
            table: "Places");

        // Drop indexes
        migrationBuilder.DropIndex(
            name: "IX_GroupTypes_ClubId",
            table: "GroupTypes");

        migrationBuilder.DropIndex(
            name: "IX_ScheduleTemplates_ClubId",
            table: "ScheduleTemplates");

        migrationBuilder.DropIndex(
            name: "IX_Groups_ClubId",
            table: "Groups");

        migrationBuilder.DropIndex(
            name: "IX_Places_ClubId",
            table: "Places");

        // Drop ClubId columns
        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "GroupTypes");

        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "ScheduleTemplates");

        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "Groups");

        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "Places");

        migrationBuilder.DropColumn(
            name: "ClubId",
            table: "AspNetUsers");

        // Drop Clubs table
        migrationBuilder.DropTable(
            name: "Clubs");
    }
}

