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
    public static class MarketDataDTO
    {
        public static Tick DataToTick(DataEvent<BinanceStreamAggregatedTrade> data)
        {
            return new Tick { 
                Id= data.Data.Id,
                Price = data.Data.Price,
                Quantity= data.Data.Quantity,
                BuyerIsMaker= data.Data.BuyerIsMaker,
                Symbol= data.Data.Symbol,
                TradeTime = data.Data.TradeTime
            };
        }
    }
}
