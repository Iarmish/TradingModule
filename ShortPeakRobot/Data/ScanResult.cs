using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class ScanResult
    {
        public long Id { get; set; }
        public long ScanId { get; set; }
        public int Index { get; set; }
        public double Profit { get; set; }
        public double Drawdown { get; set; }
        public double ProfitFactor { get; set; }
        public double RecoveryFactor { get; set; }


    }
}
