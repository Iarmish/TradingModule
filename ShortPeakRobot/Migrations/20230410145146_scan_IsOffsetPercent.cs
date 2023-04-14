using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class scan_IsOffsetPercent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffsetPercent",
                table: "Scans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOffsetPercent",
                table: "Scans");
        }
    }
}
