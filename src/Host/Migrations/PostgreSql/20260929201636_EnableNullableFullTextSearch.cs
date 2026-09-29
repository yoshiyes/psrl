using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Passerelle.Host.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class EnableNullableFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Links",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Links",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true,
                oldComputedColumnSql: "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                oldStored: true);
        }
    }
}
