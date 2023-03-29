﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using ShortPeakRobot.Data;

#nullable disable

namespace ShortPeakRobot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20230324054017_test3")]
    partial class test3
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.13")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ShortPeakRobot.Data.RobotLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ClientId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("RobotId")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("RobotLogs");
                });

            modelBuilder.Entity("ShortPeakRobot.Data.RobotOrder", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ClientId")
                        .HasColumnType("bigint");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<long>("OrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("PlacedTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<int>("RobotId")
                        .HasColumnType("integer");

                    b.Property<int>("Side")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<decimal?>("StopPrice")
                        .HasColumnType("numeric");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("RobotOrders");
                });

            modelBuilder.Entity("ShortPeakRobot.Data.RobotState", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ClientId")
                        .HasColumnType("integer");

                    b.Property<decimal>("OpenPositionPrice")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Position")
                        .HasColumnType("numeric");

                    b.Property<int>("RobotId")
                        .HasColumnType("integer");

                    b.Property<long>("SignalBuyOrderId")
                        .HasColumnType("bigint");

                    b.Property<long>("SignalSellOrderId")
                        .HasColumnType("bigint");

                    b.Property<long>("StopLossOrderId")
                        .HasColumnType("bigint");

                    b.Property<long>("TakeProfitOrderId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("RobotStates");
                });

            modelBuilder.Entity("ShortPeakRobot.Data.RobotTrade", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool>("Buyer")
                        .HasColumnType("boolean");

                    b.Property<long>("ClientId")
                        .HasColumnType("bigint");

                    b.Property<decimal>("Fee")
                        .HasColumnType("numeric");

                    b.Property<long>("OrderId")
                        .HasColumnType("bigint");

                    b.Property<int>("PositionSide")
                        .HasColumnType("integer");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<decimal>("RealizedPnl")
                        .HasColumnType("numeric");

                    b.Property<int>("RobotId")
                        .HasColumnType("integer");

                    b.Property<int>("Side")
                        .HasColumnType("integer");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("RobotTrades");
                });

            modelBuilder.Entity("ShortPeakRobot.Data.Test", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Algorithm")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("BuyAllowed")
                        .HasColumnType("boolean");

                    b.Property<int>("ClientId")
                        .HasColumnType("integer");

                    b.Property<string>("Comission")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Deposit")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("DrawDown")
                        .HasColumnType("numeric");

                    b.Property<string>("EndDate")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Interval")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsRevers")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsSlPercent")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsTpPercent")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsVariabaleLot")
                        .HasColumnType("boolean");

                    b.Property<string>("Offset")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Profit")
                        .HasColumnType("numeric");

                    b.Property<decimal>("ProfitFactor")
                        .HasColumnType("numeric");

                    b.Property<decimal>("RecoveryFactor")
                        .HasColumnType("numeric");

                    b.Property<bool>("SellAllowed")
                        .HasColumnType("boolean");

                    b.Property<string>("StartDate")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("StopLoss")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TakeProfit")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TradeHours")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Tests");
                });

            modelBuilder.Entity("ShortPeakRobot.Data.TesterTrade", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("ClientId")
                        .HasColumnType("bigint");

                    b.Property<decimal>("Fee")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<decimal>("RealizedPnl")
                        .HasColumnType("numeric");

                    b.Property<int>("Side")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("StartPrice")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("StopDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("StopPrice")
                        .HasColumnType("numeric");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TestId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("TesterTrades");
                });
#pragma warning restore 612, 618
        }
    }
}
