using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class Test : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    AlgorithmId = table.Column<int>(type: "integer", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deposit = table.Column<decimal>(type: "numeric", nullable: false),
                    StopLoss = table.Column<decimal>(type: "numeric", nullable: false),
                    TakeProfit = table.Column<decimal>(type: "numeric", nullable: false),
                    Offset = table.Column<decimal>(type: "numeric", nullable: false),
                    Comission = table.Column<decimal>(type: "numeric", nullable: false),
                    IsVariabaleLot = table.Column<bool>(type: "boolean", nullable: false),
                    IsSlPercent = table.Column<bool>(type: "boolean", nullable: false),
                    IsTpPercent = table.Column<bool>(type: "boolean", nullable: false),
                    BuyAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    SellAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    IsRevers = table.Column<bool>(type: "boolean", nullable: false),
                    TradeHours = table.Column<List<bool>>(type: "boolean[]", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    DrawDown = table.Column<decimal>(type: "numeric", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    RecoveryFactor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
