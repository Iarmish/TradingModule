using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class RobotDeal_Add_OrderPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CloseOrderPrice",
                table: "RobotDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenOrderPrice",
                table: "RobotDeals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseOrderPrice",
                table: "RobotDeals");

            migrationBuilder.DropColumn(
                name: "OpenOrderPrice",
                table: "RobotDeals");
        }
    }
}
