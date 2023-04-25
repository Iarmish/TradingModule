
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels;

namespace ShortPeakRobot.Robots.Algorithms.Services
{
    public static class ShortPeakAnalyse
    {
        public static decimal HighPeakAnalyse(ShortPeakModel highPeak, decimal price)
        {
            decimal peak = 0;
            //------------инициализация high1------------
            if (highPeak.FirstCandle == 0 && price != 0) { highPeak.FirstCandle = price; }
            //--------------добавление high2 или обновление high1 ---------------------					
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle == 0 && price > highPeak.FirstCandle)
            {
                highPeak.SecondCandle = price;
            }
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle == 0 && price < highPeak.FirstCandle)
            {
                highPeak.FirstCandle = price;
            }
            //---------- добавление short hi или обновление high1 high2
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle != 0 && price < highPeak.SecondCandle)
            {
                peak = highPeak.SecondCandle;
                highPeak.FirstCandle = price;
                highPeak.SecondCandle = 0;
            }
            if (highPeak.FirstCandle != 0 && highPeak.SecondCandle != 0 && price > highPeak.SecondCandle)
            {
                highPeak.FirstCandle = highPeak.SecondCandle;
                highPeak.SecondCandle = price;
            }

            return peak;
        }

        public static decimal LowPeakAnalyse(ShortPeakModel lowPeak, decimal price)
        {
            decimal peak = 0;
            //------------инициализация lowPeak------------
            if (lowPeak.FirstCandle == 0 && price != 0) { lowPeak.FirstCandle = price; }
            //--------------добавление low2 или обновление low1 ---------------------
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle == 0 && price < lowPeak.FirstCandle)
            {
                lowPeak.SecondCandle = price;
            }
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle == 0 && price > lowPeak.FirstCandle)
            {
                lowPeak.FirstCandle = price;
            }
            //---------- добавление short low или обновление low1 low2
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle != 0 && price > lowPeak.SecondCandle)
            {
                peak = lowPeak.SecondCandle;
                lowPeak.FirstCandle = price;
                lowPeak.SecondCandle = 0;
            }
            if (lowPeak.FirstCandle != 0 && lowPeak.SecondCandle != 0 && price < lowPeak.SecondCandle)
            {
                lowPeak.FirstCandle = lowPeak.SecondCandle;
                lowPeak.SecondCandle = price;
            }
            return peak;
        }
    }
}
