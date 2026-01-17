using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSitemapEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SitemapEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ChangeFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Priority = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastOperationUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitemapEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_Culture",
                table: "SitemapEntries",
                column: "Culture");

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_IsEnabled",
                table: "SitemapEntries",
                column: "IsEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SitemapEntries");
        }
    }
}
