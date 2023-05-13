using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class PeriodRobotDataRequest
    {
        public int robot_id { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public int limit { get; set; }
    }
}
