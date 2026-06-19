using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnPublicationTrends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: dedupe — keep newest LastUpdated per (KeywordId, TopicId, TrendYear)
            migrationBuilder.Sql(@"
WITH dups AS (
    SELECT TrendId,
           ROW_NUMBER() OVER (
               PARTITION BY KeywordId, TopicId, TrendYear
               ORDER BY LastUpdated DESC, TrendId DESC
           ) AS rn
    FROM PublicationTrends
)
DELETE FROM PublicationTrends
WHERE TrendId IN (SELECT TrendId FROM dups WHERE rn > 1);
");

            // Step 2: filtered unique indexes — keyword-scoped and topic-scoped rows are mutually exclusive
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_PublicationTrends_Keyword_Year
    ON PublicationTrends(KeywordId, TrendYear)
    WHERE KeywordId IS NOT NULL AND TopicId IS NULL;
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_PublicationTrends_Topic_Year
    ON PublicationTrends(TopicId, TrendYear)
    WHERE TopicId IS NOT NULL AND KeywordId IS NULL;
");

            // Step 3: covering index for the dashboard / activity-score snapshot read path
            migrationBuilder.Sql(@"
CREATE INDEX IX_TrendSnapshots_Date_Keyword_Topic
    ON TrendSnapshots(SnapshotDate DESC)
    INCLUDE (KeywordId, TopicId, TrendScore);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TrendSnapshots_Date_Keyword_Topic ON TrendSnapshots;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS UX_PublicationTrends_Topic_Year ON PublicationTrends;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS UX_PublicationTrends_Keyword_Year ON PublicationTrends;");
        }
    }
}
