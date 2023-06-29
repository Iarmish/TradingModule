using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms.Models
{
    public class RobotData
    {
        public decimal SignalBuyPrice { get; set; }
        public decimal SignalSellPrice { get; set; }
        public bool IsBuyOrderPlaced { get; set; }
        public bool IsSellOrderPlaced { get; set; }
    }
}
