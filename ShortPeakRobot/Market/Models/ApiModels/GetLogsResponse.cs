﻿using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class GetLogsResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<RobotLog> data { get; set; } = new List<RobotLog>();
    }
}
