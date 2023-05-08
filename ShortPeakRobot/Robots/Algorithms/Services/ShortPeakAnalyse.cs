
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels;

namespace ShortPeakRobot.Robots.Algorithms.Services
{
    public static class ShortPeakAnalyse
    {
        public static Peak HighPeakAnalyse(ShortPeakModel highPeak, Candle candle)
        {
            Peak peak = new();
            //------------инициализация high1------------
            if (highPeak.FirstCandle == 0 && candle.HighPrice != 0) { highPeak.FirstCandle = candle.HighPrice; }
            //--------------добавление high2 или обновление high1 ---------------------					
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle == 0 && candle.HighPrice > highPeak.FirstCandle)
            {
                highPeak.SecondCandle = candle.HighPrice;
                highPeak.SecondCandleDate = candle.OpenTime;
            }
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle == 0 && candle.HighPrice < highPeak.FirstCandle)
            {
                highPeak.FirstCandle = candle.HighPrice;
            }
            //---------- добавление short hi или обновление high1 high2
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle != 0 && candle.HighPrice < highPeak.SecondCandle)
            {
                peak.Volume = highPeak.SecondCandle;
                peak.Date = highPeak.SecondCandleDate;
                highPeak.FirstCandle = candle.HighPrice;
                highPeak.SecondCandle = 0;
            }
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle != 0 && candle.HighPrice > highPeak.SecondCandle)
            {
                highPeak.FirstCandle = highPeak.SecondCandle;
                highPeak.SecondCandle = candle.HighPrice;
                highPeak.SecondCandleDate = candle.OpenTime;
            }

            return peak;
        }

        public static Peak LowPeakAnalyse(ShortPeakModel lowPeak, Candle candle)
        {
            Peak peak = new();
            //------------инициализация lowPeak------------
            if (lowPeak.FirstCandle == 0 && candle.LowPrice != 0) { lowPeak.FirstCandle = candle.LowPrice; }
            //--------------добавление low2 или обновление low1 ---------------------
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle == 0 && candle.LowPrice < lowPeak.FirstCandle)
            {
                lowPeak.SecondCandle = candle.LowPrice;
                lowPeak.SecondCandleDate = candle.OpenTime;
            }
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle == 0 && candle.LowPrice > lowPeak.FirstCandle)
            {
                lowPeak.FirstCandle = candle.LowPrice;
            }
            //---------- добавление short low или обновление low1 low2
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle != 0 && candle.LowPrice > lowPeak.SecondCandle)
            {
                peak.Volume = lowPeak.SecondCandle;
                peak.Date = lowPeak.SecondCandleDate;
                lowPeak.FirstCandle = candle.LowPrice;
                lowPeak.SecondCandle = 0;
            }
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle != 0 && candle.LowPrice < lowPeak.SecondCandle)
            {
                lowPeak.FirstCandle = lowPeak.SecondCandle;
                lowPeak.SecondCandle = candle.LowPrice;
                lowPeak.SecondCandleDate = candle.OpenTime;
            }
            return peak;
        }
    }
}
