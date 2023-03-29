using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Constants
{
    public static class OrderTypes
    {
        public static Dictionary<int, string> Types = new Dictionary<int, string>()
        {
            { 0, "Limit"},
            { 1, "Market"},
            { 2, "Stop"},
            { 3, "StopMarket"},
            { 4, "TakeProfit"},
            { 5, "TakeProfitMarket"},
            { 6, "TrailingStopMarket"},
            { 7, "Liquidation"},
        };
    }
}
