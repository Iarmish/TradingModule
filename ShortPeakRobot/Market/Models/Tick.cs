using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class Tick
    {
        public long Id { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string Symbol { get; set; }
        public DateTime TradeTime { get; set; }
        public bool BuyerIsMaker { get; set; }
    }
}
