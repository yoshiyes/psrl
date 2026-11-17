using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Passerelle.Host.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GalleryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ThumbnailFileName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PreviewFileName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    CameraMake = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CameraModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LensModel = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    FocalLength = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Aperture = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ShutterSpeed = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Iso = table.Column<int>(type: "INTEGER", nullable: true),
                    TakenAt = table.Column<string>(type: "TEXT", nullable: true),
                    LocationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GalleryItemTags",
                columns: table => new
                {
                    GalleryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImageTagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryItemTags", x => new { x.GalleryItemId, x.ImageTagId });
                    table.ForeignKey(
                        name: "FK_GalleryItemTags_GalleryItems_GalleryItemId",
                        column: x => x.GalleryItemId,
                        principalTable: "GalleryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryItemTags_ImageTags_ImageTagId",
                        column: x => x.ImageTagId,
                        principalTable: "ImageTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GalleryItemTags_ImageTagId",
                table: "GalleryItemTags",
                column: "ImageTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_Name",
                table: "ImageTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_Slug",
                table: "ImageTags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GalleryItemTags");

            migrationBuilder.DropTable(
                name: "GalleryItems");

            migrationBuilder.DropTable(
                name: "ImageTags");
        }
    }
}
