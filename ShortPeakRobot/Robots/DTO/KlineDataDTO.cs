using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class KlineDataDTO
    {
        public static Candle DataToCandle(DataEvent<IBinanceStreamKlineData> data)
        {
            return new Candle
            {
                OpenPrice = data.Data.Data.OpenPrice,
                HighPrice = data.Data.Data.HighPrice,
                LowPrice = data.Data.Data.LowPrice,
                ClosePrice = data.Data.Data.ClosePrice,
                OpenTime = data.Data.Data.OpenTime,
                CloseTime = data.Data.Data.CloseTime,
                Symbol = data.Data.Symbol,
                Volume = data.Data.Data.Volume
            };
        }
    }
}
