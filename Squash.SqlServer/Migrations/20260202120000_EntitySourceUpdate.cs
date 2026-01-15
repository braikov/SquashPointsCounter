using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.SqlServer.Migrations
{
    public partial class EntitySourceUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TournamentSource",
                table: "Tournaments",
                newName: "EntitySourceId");

            migrationBuilder.AddColumn<int>(
                name: "EntitySourceId",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntitySourceId",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "EntitySourceId",
                table: "Tournaments",
                newName: "TournamentSource");
        }
    }
}
