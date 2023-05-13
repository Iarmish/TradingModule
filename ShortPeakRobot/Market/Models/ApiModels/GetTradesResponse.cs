using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiDataModels;
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
        public List<ApiTradeModel> data { get; set; } = new List<ApiTradeModel>();
    }
}
