using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiDataModels
{
    public class ApiLogModel
    {
        public long id { get; set; }
        public string date { get; set; }
        public long robot_id { get; set; } = -1;
        public long client_id { get; set; } = -1;
        public int type { get; set; }
        public string message { get; set; }

        public ApiLogModel()
        {            
            date = string.Empty;
            robot_id = 0;
            client_id = 0;
            type = 0;
            message = string.Empty;
        }

        public ApiLogModel(RobotLog log)
        {
            date = log.Date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            robot_id = log.RobotId;
            client_id = log.ClientId;
            type = log.Type;
            message = log.Message;
        }
    }
}
