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
            { 0, new RobotBaseModel{Id = 0, Name= "ShortPeak-ETH-D1", Symbol = "ETHUSDT", AlgorithmName = "ShortPeak"} },
            { 1, new RobotBaseModel{Id = 1, Name= "ShortPeak-BNB-D1", Symbol = "BNBUSDT", AlgorithmName = "ShortPeak"} },
            { 2, new RobotBaseModel{Id = 2, Name= "ShortPeak-SOL-D1", Symbol = "SOLUSDT" , AlgorithmName = "ShortPeak"} }, 
            { 3, new RobotBaseModel{Id = 3, Name= "ShortPeak-BTC-D1", Symbol = "BTCUSDT" , AlgorithmName = "ShortPeak"} },            
            { 4, new RobotBaseModel{Id = 4, Name= "ShortPeak-XRP-D1", Symbol = "XRPUSDT" , AlgorithmName = "ShortPeak"} },            
            { 5, new RobotBaseModel{Id = 5, Name= "ShortPeak-ADA-D1", Symbol = "ADAUSDT" , AlgorithmName = "ShortPeak"} },            
            { 6, new RobotBaseModel{Id = 6, Name= "ShortPeak-DOGE-D1", Symbol = "DOGEUSDT" , AlgorithmName = "ShortPeak"} },            
            { 7, new RobotBaseModel{Id = 7, Name= "LastDayHL", Symbol = "ETHUSDT" , AlgorithmName = "LastDayHL"} },            
        };

        public static int ClientId = 2;
    }
}
