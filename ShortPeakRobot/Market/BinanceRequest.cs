using ShortPeakRobot.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market
{
    public class BinanceRequest
    {
        public int RobotId { get; set; }
        public long StartDealOrderId { get; set; }        
        public int TryCount { get; set; }
        public string Symbol { get; set; } = "";
        public int Side { get; set; }
        public int OrderType { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal StopPrice { get; set; }
        public RobotOrderType  robotOrderType { get; set; }
        public RobotRequestType  robotRequestType { get; set; }
        public long  OrderId { get; set; }

    }
}
