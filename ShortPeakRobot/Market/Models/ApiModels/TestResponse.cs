using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class TestResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public TestResponseData data { get; set; } = new TestResponseData();
    }

    public class TestResponseData
    {
        public int user_id { get; set; }
        
    }
}
