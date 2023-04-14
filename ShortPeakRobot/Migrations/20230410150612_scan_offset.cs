using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class scan_offset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Offset",
                table: "Scans",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Offset",
                table: "Scans");
        }
    }
}
