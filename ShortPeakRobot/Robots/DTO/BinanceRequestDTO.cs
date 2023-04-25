using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class BinanceRequestDTO
    {
        public static BinanceRequest DTO(BinanceRequest request)
        {
            return new BinanceRequest
            {
                RobotId = request.RobotId,
                StartDealOrderId = request.StartDealOrderId,
                Symbol = request.Symbol,
                Side = request.Side,
                OrderType = request.OrderType,
                Quantity = request.Quantity,
                Price = request.Price,
                StopPrice = request.StopPrice,
                robotOrderType = request.robotOrderType,
                robotRequestType = request.robotRequestType,
                TryCount = request.TryCount,
                OrderId = request.OrderId,
            };
        }
    }
}
