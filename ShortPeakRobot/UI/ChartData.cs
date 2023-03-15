using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShortPeakRobot.UI
{
    public class ChartData
    {
        public int TimeFrame { get; set; } = 300;
        public int CandleCnt { get; set; } = 100;
        public int CandleStartIndex { get; set; }
        public int IndexMove { get; set; }
        public Point MousePosition { get; set; } = new Point();
        public List<Candle> Candles { get; set;} = new List<Candle>();
        public List<RobotTrade> Trades { get; set;} = new List<RobotTrade>();
    }
}
