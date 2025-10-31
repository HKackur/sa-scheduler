using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchedulerMVP.Data;

namespace SchedulerMVP.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20251001120000_BackfillCustomAreaCoverage")]
public partial class BackfillCustomAreaCoverage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH RECURSIVE area_with_leaf (AreaId, LeafId, ParentAreaId) AS (
                SELECT a.Id, al.LeafId, a.ParentAreaId
                FROM AreaLeafs al
                JOIN Areas a ON a.Id = al.AreaId
                UNION ALL
                SELECT parent.Id, awl.LeafId, parent.ParentAreaId
                FROM area_with_leaf awl
                JOIN Areas parent ON parent.Id = awl.ParentAreaId
            )
            INSERT INTO AreaLeafs (AreaId, LeafId)
            SELECT DISTINCT awl.AreaId, awl.LeafId
            FROM area_with_leaf awl
            LEFT JOIN AreaLeafs existing
                ON existing.AreaId = awl.AreaId
               AND existing.LeafId = awl.LeafId
            WHERE existing.AreaId IS NULL;
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op: data backfill only
    }
}
