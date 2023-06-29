using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class DealCell
    {
        public long Id { get; set; }
        public long ClientId { get; set; }
        public long TesterTradeId { get; set; }
        public DateTime Time { get; set; }
        public decimal Profit { get; set; }
    }
}
