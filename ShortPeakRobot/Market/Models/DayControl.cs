using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class DayControl
    {
        public bool IsSLTPTaken { get; set; }
        public int CurrentDay { get; set; } = DateTime.UtcNow.Day;
        public decimal DayTP { get; set; } = 5m;
        public decimal DaySL { get; set; } = -5m;
    }
}
