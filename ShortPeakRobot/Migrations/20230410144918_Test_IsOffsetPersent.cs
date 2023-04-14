using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class Test_IsOffsetPersent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffsetPercent",
                table: "Tests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOffsetPercent",
                table: "Tests");
        }
    }
}
