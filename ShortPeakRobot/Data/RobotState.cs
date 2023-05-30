using ShortPeakRobot.Constants;
using ShortPeakRobot.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class RobotState
    {
        public int Id { get; set; }
        public int RobotId { get; set; }
        public int ClientId { get; set; }
        public decimal Position { get; set; }
        public decimal OpenPositionPrice { get; set; }
        public long SignalBuyOrderId { get; set; }
        public long SignalSellOrderId { get; set; }
        public long StopLossOrderId { get; set; }
        public long TakeProfitOrderId { get; set; }

    }
}
