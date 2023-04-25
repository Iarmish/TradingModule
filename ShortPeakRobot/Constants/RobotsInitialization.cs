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
        public static readonly Dictionary<int, RobotBaseModel> dictionary = new Dictionary<int, RobotBaseModel>()
        {
            //{ 0, new RobotBaseModel{Id = 0, Name= "ShortPeak-ETH-D1", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} },
            //{ 1, new RobotBaseModel{Id = 1, Name= "LastDayHL-BNB-D1", Symbol = "BNBUSDT", AlgorithmName = "LastDayHL"} },
            //{ 2, new RobotBaseModel{Id = 2, Name= "LastDayHL-ETH-D1", Symbol = "ETHUSDT" , AlgorithmName = "LastDayHL"} }, 
            //{ 3, new RobotBaseModel{Id = 3, Name= "LastDayHL-BNB-D2", Symbol = "BNBUSDT" , AlgorithmName = "LastDayHL"} },             
            //{ 4, new RobotBaseModel{Id = 4, Name= "ShortPeak-BTC-D1", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} },              
            //{ 5, new RobotBaseModel{Id = 5, Name= "ShortPeak-ETH-D2", Symbol = "ETHUSDT" , AlgorithmName = "ShortPeak"} },            
            //{ 6, new RobotBaseModel{Id = 6, Name= "ShortPeak-ETH-D3", Symbol = "ETHUSDT" , AlgorithmName = "ShortPeak"} },             
            //{ 7, new RobotBaseModel{Id = 7, Name= "LastDayHL-SOL-D1", Symbol = "SOLUSDT" , AlgorithmName = "LastDayHL"} },            
            //{ 8, new RobotBaseModel{Id = 8, Name= "ShortPeak-ETH-D4", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} },            
            //{ 9, new RobotBaseModel{Id = 9, Name= "ShortPeak-BTC-D2", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} },            
            //{ 10, new RobotBaseModel{Id = 10, Name= "LastDayHL-LTC-D1", Symbol = "LTCUSDT" , AlgorithmName = "LastDayHL"} },            
            //{ 11, new RobotBaseModel{Id = 11, Name= "ShortPeak-ETH-D5", Symbol = "ETHUSDT" , AlgorithmName = "ShortPeak"} },            
            //{ 12, new RobotBaseModel{Id = 12, Name= "ShortPeak-BTC-D3", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} },            
            //{ 13, new RobotBaseModel{Id = 13, Name= "ShortPeak-SOL-D1", Symbol = "SOLUSDT" , AlgorithmName = "ShortPeak"} },            
            //{ 14, new RobotBaseModel{Id = 14, Name= "ShortPeak-BNB-D1", Symbol = "BNBUSDT" , AlgorithmName = "ShortPeak"} },
            //{ 15, new RobotBaseModel{Id = 15, Name= "ShortPeak-BTC-D4", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} },
            //{ 16, new RobotBaseModel{Id = 16, Name= "LastDayHL-ADA-D1", Symbol = "ADAUSDT" , AlgorithmName = "LastDayHL"} },
            //{ 17, new RobotBaseModel{Id = 17, Name= "ShortPeak-ADA-D1", Symbol = "ADAUSDT" , AlgorithmName = "ShortPeak"} },
            { 18, new RobotBaseModel{Id = 18, Name= "ShortPeak-XRP-D1", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} },
            { 19, new RobotBaseModel{Id = 19, Name= "ShortPeak-XRP-D2", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} },
            { 20, new RobotBaseModel{Id = 20, Name= "ShortPeak-XRP-D3", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} },
            { 21, new RobotBaseModel{Id = 21, Name= "VWAPHL-BTC-D1", Symbol = "BTCUSDT" , AlgorithmName = "VWAPHL"} },
            { 22, new RobotBaseModel{Id = 22, Name= "SL3-ETH-D1", Symbol = "ETHUSDT" , AlgorithmName = "SL3"} },
        };


        


        public static int ClientId = 3;
    }
}
