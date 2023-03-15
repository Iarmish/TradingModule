using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class BinTradeDTO
    {
        public static List<BinTrade> TradesDTO(WebCallResult<IEnumerable<BinanceFuturesUsdtTrade>> tardes)
        {
            var binOrders = new List<BinTrade>();



            tardes.Data.ToList().ForEach(trade =>
            {
                binOrders.Add(new BinTrade
                {                    
                    OrderId = trade.OrderId,
                    Symbol = trade.Symbol,
                    Side = (int)trade.Side,
                    PositionSide = (int)trade.PositionSide,
                    Buyer = trade.Buyer,
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    RealizedPnl = trade.RealizedPnl,
                    Timestamp = trade.Timestamp,
                    Fee = trade.Fee
                });
            });

            return binOrders;
        }
    }
}
