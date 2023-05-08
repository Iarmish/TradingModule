using Newtonsoft.Json.Linq;
using ShortPeakRobot.Robots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Constants
{
    public static class RobotsInitialization
    {
        public static readonly List<RobotBaseModel> Robots = new List<RobotBaseModel>()
        {
            // new RobotBaseModel{Id = 100, Name= "ShortPeak-ETH-D1", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} ,
            // new RobotBaseModel{Id = 1, Name= "LastDayHL-BNB-D1", Symbol = "BNBUSDT", AlgorithmName = "LastDayHL"} ,
            // new RobotBaseModel{Id = 2, Name= "LastDayHL-ETH-D1", Symbol = "ETHUSDT" , AlgorithmName = "LastDayHL"} , 
            // new RobotBaseModel{Id = 3, Name= "LastDayHL-BNB-D2", Symbol = "BNBUSDT" , AlgorithmName = "LastDayHL"} ,             
            // new RobotBaseModel{Id = 4, Name= "ShortPeak-BTC-D1", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} ,              
            // new RobotBaseModel{Id = 5, Name= "ShortPeak-ETH-D2", Symbol = "ETHUSDT" , AlgorithmName = "ShortPeak"} ,            
            //new RobotBaseModel{Id = 6, Name= "ShortPeak-ETH-D3", Symbol = "ETHUSDT" , AlgorithmName = "ShortPeak"} ,             
            //new RobotBaseModel{Id = 7, Name= "LastDayHL-SOL-D1", Symbol = "SOLUSDT" , AlgorithmName = "LastDayHL"} ,            
            //new RobotBaseModel{Id = 8, Name= "ShortPeak-ETH-D4", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} ,            
            //new RobotBaseModel{Id = 9, Name= "ShortPeak-BTC-D2", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} ,            
            // new RobotBaseModel{Id = 10, Name= "LastDayHL-LTC-D1", Symbol = "LTCUSDT" , AlgorithmName = "LastDayHL"} ,                        
            // new RobotBaseModel{Id = 12, Name= "ShortPeak-BTC-D3", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} ,            
            // new RobotBaseModel{Id = 13, Name= "ShortPeak-SOL-D1", Symbol = "SOLUSDT" , AlgorithmName = "ShortPeak"} ,            
            // new RobotBaseModel{Id = 14, Name= "ShortPeak-BNB-D1", Symbol = "BNBUSDT" , AlgorithmName = "ShortPeak"},
            // new RobotBaseModel{Id = 15, Name= "ShortPeak-BTC-D4", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"},
            // new RobotBaseModel{Id = 16, Name= "LastDayHL-ADA-D1", Symbol = "ADAUSDT" , AlgorithmName = "LastDayHL"},
            // new RobotBaseModel{Id = 17, Name= "ShortPeak-ADA-D1", Symbol = "ADAUSDT" , AlgorithmName = "ShortPeak"},
            // new RobotBaseModel{Id = 18, Name= "ShortPeak-XRP-D1", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} ,
            // new RobotBaseModel{Id = 19, Name= "ShortPeak-XRP-D2", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} ,
            //new RobotBaseModel{Id = 20, Name= "ShortPeak-XRP-D3", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} ,
            //new RobotBaseModel { Id = 21, Name = "VWAPHL-BTC-D1", Symbol = "ETHUSDT", AlgorithmName = "VWAPHL" },
            //new RobotBaseModel { Id = 22, Name = "SL3-ETH-D1", Symbol = "ETHUSDT", AlgorithmName = "SL3" },

            new RobotBaseModel{Id = 1, Name= "BNB(LDHL)", Symbol = "BNBUSDT", AlgorithmName = "LastDayHL"} ,
            new RobotBaseModel{Id = 10, Name= "LTC(LDHL)", Symbol = "LTCUSDT", AlgorithmName = "LastDayHL"} ,
            new RobotBaseModel{Id = 103, Name= "XRP(LDHL+10)", Symbol = "XRPUSDT", AlgorithmName = "LastDayHL10"} ,
            new RobotBaseModel{Id = 104, Name= "BNB(VWAP)", Symbol = "BNBUSDT", AlgorithmName = "VWAPHL"} ,
            new RobotBaseModel{Id = 5, Name= "ETH(SP)", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 2, Name= "ETH(LDHL)", Symbol = "ETHUSDT", AlgorithmName = "LastDayHL"} ,
            new RobotBaseModel{Id = 107, Name= "ETH(SP)", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 108, Name= "LTC(LDHL+10)", Symbol = "LTCUSDT", AlgorithmName = "LastDayHL10"} ,
            new RobotBaseModel{Id = 17, Name= "ADA(SP)", Symbol = "ADAUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 4, Name= "BTC(SP)", Symbol = "BTCUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 15, Name= "BTC(SP)", Symbol = "BTCUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 112, Name= "ETH(LDHL+10)", Symbol = "ETHUSDT", AlgorithmName = "LastDayHL10"} ,
            new RobotBaseModel{Id = 6, Name= "ETH(SP)", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 12, Name= "BTC(SP)", Symbol = "BTCUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 18, Name= "XRP(SP)", Symbol = "XRPUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 116, Name= "BNB(SL3)", Symbol = "BNBUSDT", AlgorithmName = "SL3"} ,
            new RobotBaseModel{Id = 117, Name= "BTC(LDHL+10)", Symbol = "BTCUSDT", AlgorithmName = "LastDayHL10"} ,
            new RobotBaseModel{Id = 118, Name= "ADA(SL3)", Symbol = "ADAUSDT", AlgorithmName = "SL3"} ,
            new RobotBaseModel{Id = 119, Name= "CHZ(VWAP)", Symbol = "CHZUSDT", AlgorithmName = "VWAPHL"} ,
            new RobotBaseModel{Id = 120, Name= "LTC(VWAP)", Symbol = "LTCUSDT", AlgorithmName = "VWAPHL"} ,
            new RobotBaseModel{Id = 121, Name= "BTC(SP+Time)", Symbol = "BTCUSDT", AlgorithmName = "ShortPeakPlusTime"} ,
            new RobotBaseModel{Id = 16, Name= "ADA(LDHL)", Symbol = "ADAUSDT", AlgorithmName = "LastDayHL"} ,
            new RobotBaseModel{Id = 19, Name= "XRP(SP)", Symbol = "XRPUSDT", AlgorithmName = "ShortPeak"} ,
            new RobotBaseModel{Id = 20, Name= "XRP(SP)", Symbol = "XRPUSDT", AlgorithmName = "ShortPeak"} ,

        };


        


        //public static int ClientId = 1;
        public static int ClientId = 3;
        //public static int ClientId = 4;
    }
}
