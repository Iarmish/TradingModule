using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class robotState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<decimal>(type: "numeric", nullable: false),
                    OpenPositionPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SignalBuyOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SignalSellOrderId = table.Column<long>(type: "bigint", nullable: false),
                    StopLossOrderId = table.Column<long>(type: "bigint", nullable: false),
                    TakeProfitOrderId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotStates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotStates");
        }
    }
}
