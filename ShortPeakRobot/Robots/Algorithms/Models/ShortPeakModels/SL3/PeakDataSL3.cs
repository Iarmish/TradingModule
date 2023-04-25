using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels.SL3
{
    public class PeakDataSL3
    {
        public decimal FirstPeak { get; set; }
        public decimal SecondPeak { get; set; }
        public decimal ThirdPeak { get; set; }
        public decimal OppositePeak { get; set; }
        public DateTime FirstPeakDate { get; set; }
        public DateTime SecondPeakDate { get; set; }
        public DateTime ThirdPeakDate { get; set; }
        public DateTime OppositePeakDate { get; set; }
    }
}
