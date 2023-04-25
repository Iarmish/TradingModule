using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class RobotTrade_add_PriceLastFilledTrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceLastFilledTrade",
                table: "RobotTrades",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceLastFilledTrade",
                table: "RobotOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceLastFilledTrade",
                table: "RobotTrades");

            migrationBuilder.DropColumn(
                name: "PriceLastFilledTrade",
                table: "RobotOrders");
        }
    }
}
