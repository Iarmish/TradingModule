using ShortPeakRobot.Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class CandleVWAPDTO
    {
        public static CandleVWAP DTO(Candle candle)
        {
            return new CandleVWAP
            {
                ClosePrice = candle.ClosePrice,
                CloseTime = candle.CloseTime,
                HighPrice = candle.HighPrice,
                LowPrice = candle.LowPrice,
                OpenPrice = candle.OpenPrice,
                OpenTime = candle.OpenTime,
                Symbol = candle.Symbol,
                Volume = candle.Volume

            };
        }

        public static CandleVWAP DTO(Candle candle, decimal vwap)
        {
            return new CandleVWAP
            {
                ClosePrice = candle.ClosePrice,
                CloseTime = candle.CloseTime,
                HighPrice = candle.HighPrice,
                LowPrice = candle.LowPrice,
                OpenPrice = candle.OpenPrice,
                OpenTime = candle.OpenTime,
                Symbol = candle.Symbol,
                Volume = candle.Volume,
                VWAP = vwap
            };
        }
    }
}
