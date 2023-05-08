using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms.Models
{
    public class RobotDealModel
    {
        public long Id { get; set; }
        public long ClientId { get; set; }
        public int RobotId { get; set; }
        public long OpenOrderId { get; set; }
        public long CloseOrderId { get; set; }
        public decimal StartDeposit { get; set; }
        public string Symbol { get; set; }
        public int Side { get; set; }
        public decimal Quantity { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal OpenOrderPrice { get; set; }
        public decimal CloseOrderPrice { get; set; }
        public decimal Fee { get; set; }
        public decimal Result { get; set; }
        public decimal Slip { get; set; }


        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
    }
}
