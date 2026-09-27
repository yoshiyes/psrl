using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Passerelle.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'unaccent_simple') THEN
                        CREATE TEXT SEARCH CONFIGURATION unaccent_simple (COPY = simple);
                        ALTER TEXT SEARCH CONFIGURATION unaccent_simple ALTER MAPPING FOR word, hword, hword_part WITH unaccent, simple;
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Links",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Links_SearchVector",
                table: "Links",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_SearchVector",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Links");

            migrationBuilder.Sql("DROP TEXT SEARCH CONFIGURATION IF EXISTS unaccent_simple;");
        }
    }
}
