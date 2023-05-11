using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class GetTradesResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<RobotTrade> data { get; set; } = new List<RobotTrade>();
    }
}
