using Binance.Net.Objects.Models.Futures;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market.Models.ApiModels;
using System.Collections.Generic;

namespace ShortPeakRobot.Market
{
    public static class MarketData
    {
        public static Dictionary<string, Dictionary<int, List<Candle>>> CandleDictionary { get; set; } =
            new Dictionary<string, Dictionary<int, List<Candle>>>();
       
        public static List<RobotOrder> OpenOrders { get; set; } = new List<RobotOrder>();        

        public static List<CandleExtParam> CandleExtParams { get; set; } = new List<CandleExtParam>();
        
        public static MarketInfo Info { get; set; } = new MarketInfo();
        public static DayControl DayStatus { get; set; } = new DayControl();
        
        public static MarketManager MarketManager { get; set; } = new MarketManager();
        


        public static List<int> LogTypeFilter { get; set; } = new List<int>();
    }

}
