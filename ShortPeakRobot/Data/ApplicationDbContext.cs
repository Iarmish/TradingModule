using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext()
        {
            Database.SetCommandTimeout(180);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=5.188.159.75;Port=5432;Database=terminal;Username=binance782;Password=U8h64NroE93g");

            base.OnConfiguring(optionsBuilder);

        }
       
        public DbSet<RobotOrder> RobotOrders { get; set; }
        public DbSet<RobotLog> RobotLogs { get; set; }
        public DbSet<RobotTrade> RobotTrades { get; set; }
        public DbSet<RobotState> RobotStates { get; set; }
        public DbSet<TesterTrade> TesterTrades { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Scan> Scans { get; set; }
        public DbSet<ScanResult>  ScanResults { get; set; }
        public DbSet<RobotDeal> RobotDeals { get; set; }
        public DbSet<DealCell> DealCells { get; set; }
        
    }
}
