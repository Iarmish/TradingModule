using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiModels
{
    public class LoginResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public LoginResponseData data { get; set; } = new LoginResponseData();
    }


    public class LoginResponseData
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }
}
