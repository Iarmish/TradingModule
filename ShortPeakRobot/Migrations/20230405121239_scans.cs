using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class scans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScanId = table.Column<long>(type: "bigint", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    Drawdown = table.Column<decimal>(type: "numeric", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    RecoveryFactor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Algorithm = table.Column<string>(type: "text", nullable: false),
                    Stock = table.Column<string>(type: "text", nullable: false),
                    TimeFrame = table.Column<string>(type: "text", nullable: false),
                    Date1 = table.Column<string>(type: "text", nullable: false),
                    Date2 = table.Column<string>(type: "text", nullable: false),
                    Deposit = table.Column<string>(type: "text", nullable: false),
                    Commission = table.Column<string>(type: "text", nullable: false),
                    TradeHours = table.Column<string>(type: "text", nullable: false),
                    FlagSell = table.Column<bool>(type: "boolean", nullable: false),
                    FlagBuy = table.Column<bool>(type: "boolean", nullable: false),
                    FlagReverse = table.Column<bool>(type: "boolean", nullable: false),
                    VariableLot = table.Column<bool>(type: "boolean", nullable: false),
                    IsSlPercent = table.Column<bool>(type: "boolean", nullable: false),
                    IsTpPercent = table.Column<bool>(type: "boolean", nullable: false),
                    SL1 = table.Column<string>(type: "text", nullable: false),
                    SL2 = table.Column<string>(type: "text", nullable: false),
                    TP1 = table.Column<string>(type: "text", nullable: false),
                    TP2 = table.Column<string>(type: "text", nullable: false),
                    StepSl = table.Column<string>(type: "text", nullable: false),
                    StepTp = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanResults");

            migrationBuilder.DropTable(
                name: "Scans");
        }
    }
}
