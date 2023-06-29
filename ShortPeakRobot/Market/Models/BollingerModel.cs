using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class BollingerModel
    {
        public decimal Ema { get; set; }
        public decimal UpLine { get; set; }
        public decimal DownLine { get; set; }
        public decimal Deviation { get; set; }
        public int Position { get; set; }


    }
}
