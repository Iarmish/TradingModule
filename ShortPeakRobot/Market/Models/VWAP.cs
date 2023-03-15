using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class VWAP
    {
        public decimal VwapSum { get; set; }
        public decimal VolumeSum { get; set; }
        public int Position { get; set; }
        public int CandleCount { get; set; }
        public decimal Volume { get; set; }
        public DateTime Date { get; set; }
    }
}
