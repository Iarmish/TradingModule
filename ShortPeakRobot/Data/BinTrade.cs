using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class BinTrade
    {
        public long Id { get; set; }
        public long OrderId { get; set; }        
        public long ClientId { get; set; }
        public int RobotId { get; set; }
        public string Symbol { get; set; }
        public int Side { get; set; }
        public int PositionSide { get; set; }
        public bool Buyer { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal RealizedPnl { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Fee { get; set; }
    }
}
