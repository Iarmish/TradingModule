﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class LoginRequest
    {
        public string login { get; set; }
        public string password { get; set; }
        public string app_instance_key { get; set; }
    }
}
