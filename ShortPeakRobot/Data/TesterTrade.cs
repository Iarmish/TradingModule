using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class TesterTrade
    {
        public long Id { get; set; }        
        public long ClientId { get; set; }
        public int TesterId { get; set; }
        public string Symbol { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime StopDate { get; set; }
        public int Side { get; set; }
        public decimal StartPrice { get; set; }
        public decimal StopPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal RealizedPnl { get; set; }
        public decimal Fee { get; set; }
    }
}
