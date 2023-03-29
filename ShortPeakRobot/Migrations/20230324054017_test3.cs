using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    public partial class test3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TradeHours",
                table: "Tests",
                type: "text",
                nullable: false,
                oldClrType: typeof(List<bool>),
                oldType: "boolean[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<bool>>(
                name: "TradeHours",
                table: "Tests",
                type: "boolean[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
